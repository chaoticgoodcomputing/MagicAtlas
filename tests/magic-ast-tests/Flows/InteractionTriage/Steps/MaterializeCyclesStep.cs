using Flowthru.Step;
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

      var ranked = engine
        .FindCycles(edges, LengthBound)
        // No 1-card combo exists in MTG — a loop whose ports all belong to one card is an artifact.
        .Where(c =>
          c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card })
            .Distinct(StringComparer.Ordinal)
            .Count() > 1
        )
        .OrderBy(c => (int)c.Tier) // GREEN verdict first
        .ThenBy(c => c.Edges.Count) // then shortest
        .ToList();

      // Dedup by node set (keep the best-ranked representative of each loop).
      var seen = new HashSet<string>(StringComparer.Ordinal);
      var deduped = ranked
        .Where(c =>
          seen.Add(
            string.Join(
              "|",
              c.Edges.SelectMany(e => new[] { e.From.Identity, e.To.Identity })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
            )
          )
        )
        .ToList();

      var total = deduped.Count;

      return deduped
        .Take(DisplayCap)
        .SelectMany(
          (cycle, index) =>
            cycle.Edges.Select(
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
                  CycleTier = cycle.Tier.ToString(),
                  Firable = cycle.Firable,
                  CoCostsSatisfied = cycle.CoCostsSatisfied,
                  LimitingReason = cycle.LimitingReason ?? "",
                  Total = total,
                }
            )
        )
        .ToList();
    };
}
