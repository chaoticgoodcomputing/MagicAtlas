using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Flows.InteractionTriage.Steps;

namespace MagicAtlas.Flows.InteractionTriage;

/// <summary>
/// The combos producer promoted out of tests/magic-ast-tests's <c>InteractionTriageFlow</c>
/// (upstream-atlas-data-plan §0/§6 P0): the single FetchCombos step that projects Commander Spellbook's
/// ~510 MB <c>variants.json</c> dump (the <c>CsbVariantsRaw</c> HTTP catalog item, fetched + cached by
/// <c>UseHttp</c>) down to the lean <c>combos.json</c> work-list — the CardAtlas D4 reconstruction input.
///
/// <para>Minimal-viable promotion: only FetchCombos moves here. The rest of the test-side
/// InteractionTriageFlow (ClassifyCombos / MaterializeCardEdges / MaterializeCycles / PortNodes / the
/// Plotly viz) depends on the mast-interaction reconstruction engine and test-only fixtures, and produces
/// diagnostics that are NOT CardAtlas pipeline inputs — so it stays in tests/magic-ast-tests.</para>
/// </summary>
public static class FetchCombosFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow(
      "FetchCombos",
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
      }
    );
}
