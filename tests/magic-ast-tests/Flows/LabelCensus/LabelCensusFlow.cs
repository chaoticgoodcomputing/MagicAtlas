using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.LabelCensus.Steps;

namespace MagicAtlas.Ast.Tests.Flows.LabelCensus;

/// <summary>
/// Port-label census (diagnostic): parses + projects every corpus card and aggregates the distinct
/// port-label space → <c>_08_Reporting/port-label-census.json</c>. Answers the "analytical-chemistry"
/// question for the two-layer cycle engine — is the atom (label) count far below the molecule (card)
/// count? Re-run as parser coverage grows; the card:label ratio is the health metric.
/// </summary>
public static class LabelCensusFlow
{
  public static BuiltFlow Create(Catalog catalog, string ontologyPath) =>
    FlowBuilder.CreateFlow(
      "PortLabelCensus",
      pipeline =>
      {
        pipeline.AddStep<IEnumerable<MastCardInput>, PortLabelCensus>(
          label: "Census",
          transform: CensusStep.Create(ontologyPath),
          inputs: catalog.CardInputs,
          outputs: catalog.PortLabelCensus
        );
      }
    );
}
