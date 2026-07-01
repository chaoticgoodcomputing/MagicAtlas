using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.PortGraphAtlas.Steps;

namespace MagicAtlas.Ast.Tests.Flows.PortGraphAtlas;

/// <summary>
/// Port-graph structural atlas (diagnostic): materializes the emergent port-LABEL graph over the CSB
/// combo-card union and analyzes its edge structure → <c>_08_Reporting/port-graph-atlas.json</c>.
/// Tests the "is the atom graph one economy-glued blob, and what cross-family cycles bridge resources"
/// question from <c>libs/mast-interaction/docs/two-layer-cycle-engine.md</c> — the edge-structure
/// complement to the node-side <c>PortLabelCensus</c>. Reads the committed <c>combos.json</c> +
/// <c>card-inputs.json</c> (offline; no Scryfall, no ParseRecords dependency). Re-run as coverage /
/// flow arms grow — new arms add label edges that can open new cross-family archetypes.
/// </summary>
public static class PortGraphAtlasFlow
{
  public static BuiltFlow Create(Catalog catalog, string ontologyPath) =>
    FlowBuilder.CreateFlow(
      "PortGraphAtlas",
      pipeline =>
      {
        pipeline.AddStep<
          IEnumerable<Combo>,
          IEnumerable<MastCardInput>,
          Data._08_Reporting.Schemas.PortGraphAtlas
        >(
          label: "PortGraphAtlas",
          transform: PortGraphAtlasStep.Create(ontologyPath),
          inputs: (catalog.Combos, catalog.CardInputs),
          outputs: catalog.PortGraphAtlas
        );
      }
    );
}
