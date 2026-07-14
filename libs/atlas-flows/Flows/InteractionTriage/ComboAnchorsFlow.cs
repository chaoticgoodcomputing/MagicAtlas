using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.InteractionTriage.Steps;

namespace MagicAtlas.Flows.InteractionTriage;

/// <summary>
/// The combo-ANCHORED pick surface (demand side): ranks the unparsed hub cards by the combo-popularity
/// value each gates, with sole-blocker counts, co-star neighborhood, and the parser-family vs
/// empty-oracle-text (DATA gap) split. Emits <c>combo-anchor-report.json</c> from three file-drop inputs
/// (<c>combos.json</c> + <c>parse-records.json</c> + <c>card-inputs.json</c>); a parallel read of the
/// same Combos + ParseRecords the interaction-triage classifier uses, plus CardInputs for type line /
/// oracle-text-emptiness. A pick surface for the mast-tdd-loop, never a gate.
///
/// <para>Promoted alongside <see cref="Steps.RankComboAnchorsStep"/> so production — not the test
/// assembly — can emit the combo-anchor report (upstream-atlas-data-plan §0/§6 P0). The upstream parse
/// pass that produces <c>parse-records.json</c> is not yet wired into atlas-flows; supply it as a
/// file-drop until then (see Program registration + the flow's catalog inputs).</para>
/// </summary>
public static class ComboAnchorsFlow
{
  public static BuiltFlow Create(Catalog catalog) =>
    FlowBuilder.CreateFlow(
      "ComboAnchors",
      pipeline =>
      {
        pipeline.AddStep<
          IEnumerable<Combo>,
          IEnumerable<ParseRecord>,
          IEnumerable<MastCardInput>,
          ComboAnchorReport
        >(
          label: "RankComboAnchors",
          transform: RankComboAnchorsStep.Create(),
          inputs: (catalog.Combos, catalog.ParseRecords, catalog.CardInputs),
          outputs: catalog.ComboAnchorReport
        );
      }
    );
}
