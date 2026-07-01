using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.PortGraphAtlas.Steps;

namespace MagicAtlas.Ast.Tests.Flows.PortGraphAtlas;

/// <summary>
/// Port-graph structural atlas (diagnostic): materializes the emergent port-LABEL graph over the CSB
/// combo-card union and analyzes its edge structure → <c>_08_Reporting/port-graph-atlas.json</c>, plus
/// the family-collapsed "subway map" (<c>_08_Reporting/family-graph.html</c>) — the ~17-station resource
/// graph with the 16 fundamental two-family engines highlighted. Tests the "is the atom graph one
/// economy-glued blob, and what cross-family cycles bridge resources" question from
/// <c>libs/mast-interaction/docs/two-layer-cycle-engine.md</c>. Reads the committed <c>combos.json</c> +
/// <c>card-inputs.json</c> (offline; no Scryfall, no ParseRecords dependency).
/// </summary>
public static class PortGraphAtlasFlow
{
  public static BuiltFlow Create(Catalog catalog, string ontologyPath, IPythonExecutor executor) =>
    FlowBuilder.CreateFlow(
      "PortGraphAtlas",
      pipeline =>
      {
        pipeline.AddStep<
          IEnumerable<Combo>,
          IEnumerable<MastCardInput>,
          Data._08_Reporting.Schemas.PortGraphAtlas,
          IEnumerable<FamilyNodeRow>,
          IEnumerable<FamilyEdgeRow>
        >(
          label: "PortGraphAtlas",
          transform: PortGraphAtlasStep.Create(ontologyPath),
          inputs: (catalog.Combos, catalog.CardInputs),
          outputs: (catalog.PortGraphAtlas, catalog.FamilyGraphNodes, catalog.FamilyGraphEdges)
        );

        // The family-collapsed "subway map" (Python: networkx layout + Plotly render). Stations = resource
        // families sized by card mass; lines = arm (physics) / wiring (card text); the 16 two-family engines
        // (bidirectional pairs) are drawn as highlighted loops.
        pipeline.AddPythonStep(
          label: "SubwayMap",
          module: "Flows.PortGraphAtlas.subway_map",
          function: "subway_map",
          input: (catalog.FamilyGraphNodes, catalog.FamilyGraphEdges),
          output: catalog.FamilyGraphHtml,
          executor: executor
        );
      }
    );
}
