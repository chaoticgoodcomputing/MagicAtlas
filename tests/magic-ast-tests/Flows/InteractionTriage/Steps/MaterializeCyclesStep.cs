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
/// which a per-edge export cannot express. Bounded to length ≤6 — the sac→death→token→doubler→refuel
/// archetype (Ashnod's Altar × Pitiless × Chatterfang) spans five hops, and the cast-recursion blink loop
/// (Displacer Kitten: emit:cast → trigger:cast → emit:blink → etb → emit:untap → pay:mana → emit:cast)
/// spans six, so the reconstruction reach is 6 hops (still tractable at corpus scale; cf. the GPU-Johnson
/// note in docs if reach must grow further). Single-card loops dropped (no 1-card combo exists in MTG), deduped by node set, ranked
/// GREEN-verdict-first then shortest, and PER-TIER display-capped (verified/partial/derived each capped
/// so all three appear — partial + derived each far exceed a flat cap). One <see cref="CycleEdgeRow"/> per hop.
/// </summary>
[FlowthruStep]
public static class MaterializeCyclesStep
{
  private const int LengthBound = 6;
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

      // Classify a loop against the CSB corpus by its MAXIMAL-OVERLAP combo (the combo sharing the most
      // of the loop's cards — not the first co-occurring pair, which can anchor onto an unrelated combo
      // that happens to share two cards while the loop is really a different, larger combo). VERIFIED
      // when the loop's cards EXACTLY match a combo; PARTIAL when the best combo shares ≥2 of them (a
      // partial reconstruction of a known combo); DERIVED otherwise (no two cards are a known combo).
      (string Match, string ComboId) Classify(PortCycle cycle)
      {
        var cards = cycle
          .Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
          .Distinct(StringComparer.Ordinal)
          .ToList();
        var cardSet = cards.ToHashSet(StringComparer.Ordinal);

        // Candidate combos: any that shares ≥1 card (via the index) — avoids a full corpus scan.
        var candidates = new HashSet<int>();
        foreach (var card in cards)
          if (cardToCombo.TryGetValue(card, out var combos))
            candidates.UnionWith(combos);
        if (candidates.Count == 0)
          return ("derived", "");

        // The best anchor: most shared cards, then the tightest combo, then lowest index (deterministic).
        var best = candidates
          .Select(i => (
            Index: i,
            Overlap: comboCards[i].Cards.Count(cardSet.Contains),
            Size: comboCards[i].Cards.Count
          ))
          .OrderByDescending(x => x.Overlap)
          .ThenBy(x => x.Size)
          .ThenBy(x => x.Index)
          .First();

        if (best.Overlap < 2)
          return ("derived", ""); // a single shared card is not a partial reconstruction
        if (best.Overlap == cards.Count && best.Size == cards.Count)
          return ("verified", comboCards[best.Index].Id); // cards == a combo, exactly
        return ("partial", comboCards[best.Index].Id); // best combo shares ≥2 (subset or overlap)
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
