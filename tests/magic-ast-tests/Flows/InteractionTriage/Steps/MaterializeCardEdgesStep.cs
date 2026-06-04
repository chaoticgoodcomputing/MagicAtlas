using Flowthru.Step;
using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

/// <summary>
/// The L2+ reconstruction step — the materialized card-level <b>union</b> interaction graph, on the
/// ADR-0002 single-role port model. Walks the ports of <em>every</em> card across the parse-ready
/// combos (a port is a card property, deduped per card — NOT siloed per combo), then runs the
/// <see cref="PortGraphEngine"/> ONCE over the whole set: it combines the walk's card-defined edges
/// (certain) with the rules-defined edges it derives (flow + the sac→death bridge + modifier), tiered
/// by the operator (<c>Disjoint</c> pruned). Cycles form from any closed loop among the union's
/// ports. Emits one flat <see cref="CardEdgeRow"/> per edge (tier-tagged); the viz finds the cycles.
/// </summary>
/// <remarks>
/// Parsing + the walk are cached per distinct card name. The walk serializes the parser's ability AST
/// to the JSON shape it dispatches over (<see cref="MagicASTJsonOptions.Strict"/>). The materialization
/// is quadratic in the union's emit/consume ports; widen to sampling/caps if the set grows large.
/// </remarks>
[FlowthruStep]
public static class MaterializeCardEdgesStep
{
  public static Func<
    (
      IEnumerable<Combo> Combos,
      IEnumerable<ParseRecord> Records,
      IEnumerable<MastCardInput> CardInputs
    ),
    IEnumerable<CardEdgeRow>
  > Create(string ontologyPath) =>
    inputs =>
    {
      var (_, edges) = InteractionUnion.Materialize(
        inputs.Combos,
        inputs.Records,
        inputs.CardInputs,
        ontologyPath
      );

      return edges
        .Select(e => new CardEdgeRow
        {
          FromCard = e.From.Card,
          FromLabel = e.From.Label,
          ToCard = e.To.Card,
          ToLabel = e.To.Label,
          Resource = ResourceOf(e),
          Family = e.Family.ToString(),
          Tier = e.Tier.ToString(),
          Reason = e.Reason ?? "",
        })
        .ToList();
    };

  /// <summary>The flowing resource, read off the source label: <c>emit:&lt;kind&gt;</c> → the kind; a sac source bridges to a death.</summary>
  private static string ResourceOf(PortEdge edge)
  {
    var parts = edge.From.Label.Split(':');
    if (parts[0] == "emit" && parts.Length > 1)
      return parts[1];
    if (parts[0] == "sac")
      return "death"; // the bridge: a sacrificed permanent dies
    return parts[0];
  }
}
