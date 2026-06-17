using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.InteractionTriage.Steps;

namespace MagicAtlas.Ast.Tests.Flows.InteractionTriage;

/// <summary>
/// Interaction-coverage triage (the mast-interaction analogue of <c>MagicAstTriage</c>). Ranks
/// Commander Spellbook combos by popularity and classifies why each is not yet reconstructable by
/// the interaction engine — routing the work to the right loop:
/// <list type="number">
///   <item>ProjectCombos → <c>_02_Intermediate</c> combos.json (CSB dump stripped to the work-list).</item>
///   <item>ClassifyCombos → <c>_08_Reporting</c> interaction-triage-report.json: each combo's first
///         blocking layer — <b>parse-blocked</b> (a card doesn't parse → mast-tdd-loop, with the
///         blocking cards named + the popularity-weighted card-gap overlay) vs <b>parse-ready</b>
///         (every card parses → the interaction loop's reconstruction queue).</item>
/// </list>
/// ClassifyCombos consumes <c>MagicAstTriage</c>'s ParseRecords, so the flow auto-slices in the parse
/// chain to (re)produce them. NEXT INCREMENT: split parse-ready combos by whether the engine already
/// reconstructs them (L2 recognizer / L3 grammar / reconstructed) — the novel fixture+recognizer+grammar work.
/// </summary>
public static class InteractionTriageFlow
{
  public static BuiltFlow Create(
    Catalog catalog,
    IPythonExecutor executor,
    string grammarPath,
    string ontologyPath
  )
  {
    return FlowBuilder.CreateFlow(
      "InteractionTriage",
      pipeline =>
      {
        // The CSB variants.json dump is fetched (and conditional-GET cached) by Flowthru's
        // HttpStorageMedium via the CsbVariantsRaw catalog item; this step just projects it.
        pipeline.AddStep<CsbVariantsDump, IEnumerable<Combo>>(
          label: "FetchCombos",
          transform: FetchCombosStep.Create(),
          inputs: catalog.CsbVariantsRaw,
          outputs: catalog.Combos
        );

        pipeline.AddStep<IEnumerable<Combo>, IEnumerable<ParseRecord>, InteractionTriageReport>(
          label: "ClassifyCombos",
          transform: ClassifyCombosStep.Create(),
          inputs: (catalog.Combos, catalog.ParseRecords),
          outputs: catalog.InteractionTriageReport
        );

        // Label-level graph (left viz subplot): the known-families grammar, flattened.
        pipeline.AddStep<IEnumerable<LabelEdgeRow>>(
          label: "LabelEdges",
          transform: LabelEdgesStep.Create(grammarPath),
          outputs: catalog.LabelEdges
        );

        // Card-level graph (right viz subplot): engine edges materialized over parse-ready combos. Emits
        // TWO outputs from the one materialization: the flat edge export AND a quick port-graph census
        // (node/edge counts + tier/family/resource split → port-graph-metrics.json). The census lands
        // here, with CardEdges — BEFORE the union-scale MaterializeCycles step, which is intractable at
        // current corpus size (the census quantifies why: a dense ~10^5-edge graph).
        pipeline.AddStep<
          IEnumerable<Combo>,
          IEnumerable<ParseRecord>,
          IEnumerable<MastCardInput>,
          IEnumerable<CardEdgeRow>,
          PortGraphMetrics
        >(
          label: "MaterializeCardEdges",
          transform: MaterializeCardEdgesStep.Create(ontologyPath),
          inputs: (catalog.Combos, catalog.ParseRecords, catalog.CardInputs),
          outputs: (catalog.CardEdges, catalog.PortGraphMetrics)
        );

        // Reconstructed cycles (right viz subplot): computed in C# by the engine, with cycle-level
        // verdict tiers (firability + multi-cost conjunction floors) the per-edge export can't carry.
        pipeline.AddStep<
          IEnumerable<Combo>,
          IEnumerable<ParseRecord>,
          IEnumerable<MastCardInput>,
          IEnumerable<CycleEdgeRow>
        >(
          label: "MaterializeCycles",
          transform: MaterializeCyclesStep.Create(ontologyPath),
          inputs: (catalog.Combos, catalog.ParseRecords, catalog.CardInputs),
          outputs: catalog.CycleEdges
        );

        // Per-card oracle text for the viz hover (reads the union edges + card inputs; no re-parse).
        pipeline.AddStep<IEnumerable<CardEdgeRow>, IEnumerable<MastCardInput>, IEnumerable<PortNodeRow>>(
          label: "PortNodes",
          transform: PortNodesStep.Create(),
          inputs: (catalog.CardEdges, catalog.CardInputs),
          outputs: catalog.PortNodes
        );

        // The Plotly viz (Python step): label grammar | reconstructed cycles (engine verdicts),
        // oracle-text hover, directed.
        pipeline.AddPythonStep(
          label: "PlotInteractionGraph",
          module: "Flows.InteractionTriage.plot_interaction_graph",
          function: "plot_interaction_graph",
          input: (catalog.LabelEdges, catalog.CycleEdges, catalog.PortNodes),
          output: catalog.InteractionGraphHtml,
          executor: executor
        );
      }
    );
  }
}
