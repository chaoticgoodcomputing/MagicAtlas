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
/// GREEN-verdict-first then shortest, and display-capped. One flat <see cref="CycleEdgeRow"/> per hop.
/// </summary>
[FlowthruStep]
public static class MaterializeCyclesStep
{
  private const int LengthBound = 5;
  private const int DisplayCap = 120;

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

      string KnownComboFor(PortCycle cycle)
      {
        List<int>? candidates = null;
        foreach (
          var card in cycle
            .Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
            .Distinct(StringComparer.Ordinal)
        )
        {
          if (!cardToCombo.TryGetValue(card, out var combos))
            return ""; // a cycle card in no combo — can't be a known combo
          candidates = candidates is null ? [.. combos] : [.. candidates.Intersect(combos)];
          if (candidates.Count == 0)
            return ""; // cards span multiple combos — a derived (cross-combo) loop
        }
        return candidates is { Count: > 0 } ? comboCards[candidates[0]].Id : "";
      }

      var ranked = engine
        .FindCycles(edges, LengthBound)
        // No 1-card combo exists in MTG — a loop whose ports all belong to one card is an artifact.
        .Where(c =>
          c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
            .Distinct(StringComparer.Ordinal)
            .Count() > 1
        )
        .Select(c => (Cycle: c, ComboId: KnownComboFor(c)))
        .OrderBy(x => x.ComboId.Length == 0 ? 1 : 0) // KNOWN verified combos first
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

      return deduped
        .Take(DisplayCap)
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
                  Known = item.ComboId.Length > 0,
                  ComboId = item.ComboId,
                  Total = total,
                }
            )
        )
        .ToList();
    };
}
