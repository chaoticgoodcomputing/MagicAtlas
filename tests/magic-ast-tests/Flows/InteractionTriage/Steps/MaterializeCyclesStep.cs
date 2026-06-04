using Flowthru.Step;
using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// The reconstructed cycles, computed in C# via the direct MAST APIs (<c>PortGraphEngine.FindCycles</c>)
/// rather than re-derived in Python from the flat edges — so the viz renders the engine's
/// <b>cycle-level verdict</b> (the worst hop floored by §8 firability + the multi-cost conjunction),
/// which a per-edge export cannot express. Bounded to length ≤5 — a full sac→death→token→doubler→refuel
/// loop spans five hops (the Ashnod's Altar × Pitiless × Chatterfang archetype), still ~3s at corpus
/// scale. Single-card loops dropped (no 1-card combo exists in MTG), deduped by node set, ranked
/// GREEN-verdict-first then shortest, and PER-TIER display-capped (verified/partial/derived each capped
/// so all three appear — partial + derived each far exceed a flat cap). One <see cref="CycleEdgeRow"/> per hop.
/// </summary>
[FlowthruStep]
public static class MaterializeCyclesStep
{
  private const int LengthBound = 5;
  private const int PerTierCap = 60;

  public static Func<
    (
      IEnumerable<Combo> Combos,
      IEnumerable<ParseRecord> Records,
      IEnumerable<MastCardInput> CardInputs
    ),
    IEnumerable<CycleEdgeRow>
  > Create(string ontologyPath) =>
    inputs =>
    {
      var (engine, edges) = InteractionUnion.Materialize(
        inputs.Combos,
        inputs.Records,
        inputs.CardInputs,
        ontologyPath
      );

      // CSB cross-check index: a card → the combos that contain it. A reconstructed cycle is a KNOWN
      // verified combo when its cards all co-occur in one Commander Spellbook combo (cycle.cards ⊆ a
      // combo) — the intersection of the per-card combo lists; else it is an engine-DERIVED loop.
      var comboCards = inputs
        .Combos.Select(c =>
          (c.Id, Cards: c.Cards.Select(x => x.Name).ToHashSet(StringComparer.Ordinal))
        )
        .ToList();
      var cardToCombo = new Dictionary<string, List<int>>(StringComparer.Ordinal);
      for (var i = 0; i < comboCards.Count; i++)
        foreach (var card in comboCards[i].Cards)
        {
          if (!cardToCombo.TryGetValue(card, out var list))
            cardToCombo[card] = list = [];
          list.Add(i);
        }

      // Classify a loop against the CSB corpus: VERIFIED when its cards EXACTLY match a combo
      // (cycle.cards == a combo); PARTIAL when they're a subset of one (a partial reconstruction of a
      // known combo); DERIVED when they span no single combo (a genuinely novel loop).
      (string Match, string ComboId) Classify(PortCycle cycle)
      {
        var cards = cycle
          .Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
          .Distinct(StringComparer.Ordinal)
          .ToList();

        // Combos containing EVERY cycle card (⊆). Exact size ⇒ verified; else a partial reconstruction.
        List<int>? superset = null;
        var allInOneCombo = true;
        foreach (var card in cards)
        {
          if (!cardToCombo.TryGetValue(card, out var combos))
          {
            allInOneCombo = false;
            break;
          }
          superset = superset is null ? [.. combos] : [.. superset.Intersect(combos)];
          if (superset.Count == 0)
          {
            allInOneCombo = false;
            break;
          }
        }
        if (allInOneCombo && superset is { Count: > 0 })
        {
          foreach (var idx in superset)
            if (comboCards[idx].Cards.Count == cards.Count)
              return ("verified", comboCards[idx].Id); // cards == a combo, exactly
          var best = superset.OrderBy(i => comboCards[i].Cards.Count).First();
          return ("partial", comboCards[best].Id); // cards ⊆ a combo
        }

        // PARTIAL on a weaker test too: ANY two of the loop's cards co-occur in a known combo (≥2
        // cards from a CSB combo). A loop that shares a known 2-card synergy is a partial reconstruction,
        // not novel — reserving "derived" for loops where no two cards are a known combo together.
        for (var i = 0; i < cards.Count; i++)
          for (var j = i + 1; j < cards.Count; j++)
            if (
              cardToCombo.TryGetValue(cards[i], out var ci)
              && cardToCombo.TryGetValue(cards[j], out var cj)
            )
            {
              var both = ci.Intersect(cj).ToList();
              if (both.Count > 0)
                return ("partial", comboCards[both[0]].Id);
            }

        return ("derived", ""); // no two cards co-occur in any combo — genuinely novel
      }

      static int Rank(string match) =>
        match switch
        {
          "verified" => 0,
          "partial" => 1,
          _ => 2,
        };

      var ranked = engine
        .FindCycles(edges, LengthBound)
        // No 1-card combo exists in MTG — a loop whose ports all belong to one card is an artifact.
        .Where(c =>
          c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
            .Distinct(StringComparer.Ordinal)
            .Count() > 1
        )
        .Select(c => (Cycle: c, Class: Classify(c)))
        .OrderBy(x => Rank(x.Class.Match)) // verified, then partial, then derived
        .ThenBy(x => (int)x.Cycle.Tier) // then GREEN verdict
        .ThenBy(x => x.Cycle.Edges.Count) // then shortest
        .ToList();

      // Dedup by node set (keep the best-ranked representative of each loop).
      var seen = new HashSet<string>(StringComparer.Ordinal);
      var deduped = ranked
        .Where(x =>
          seen.Add(
            string.Join(
              "|",
              x.Cycle.Edges.SelectMany(e => new[] { e.From.Identity, e.To.Identity })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
            )
          )
        )
        .ToList();

      var total = deduped.Count;

      // Per-tier display cap so verified / partial / derived ALL appear — partial and derived each far
      // exceed a flat cap, so a single Take would show only the top tier and hide the novel loops.
      var shown = new[] { "verified", "partial", "derived" }
        .SelectMany(m => deduped.Where(x => x.Class.Match == m).Take(PerTierCap))
        .ToList();

      return shown
        .SelectMany(
          (item, index) =>
            item.Cycle.Edges.Select(
              (hop, hopIndex) =>
                new CycleEdgeRow
                {
                  Cycle = index,
                  Hop = hopIndex,
                  FromCard = hop.From.Card,
                  FromLabel = hop.From.Label,
                  ToCard = hop.To.Card,
                  ToLabel = hop.To.Label,
                  EdgeTier = hop.Tier.ToString(),
                  CycleTier = item.Cycle.Tier.ToString(),
                  Firable = item.Cycle.Firable,
                  CoCostsSatisfied = item.Cycle.CoCostsSatisfied,
                  LimitingReason = item.Cycle.LimitingReason ?? "",
                  Match = item.Class.Match,
                  ComboId = item.Class.ComboId,
                  Total = total,
                }
            )
        )
        .ToList();
    };
}
