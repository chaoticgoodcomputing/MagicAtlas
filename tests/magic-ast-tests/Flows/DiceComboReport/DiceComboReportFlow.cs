using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.DiceComboReport.Steps;

namespace MagicAtlas.Ast.Tests.Flows.DiceComboReport;

/// <summary>
/// Dice-combo reconstruction report (DIAGNOSTIC). Reconstructs every CSB die-roll combo "as if the
/// support cards were parsed" (gold AST &gt; hand-authored stub &gt; parsed text &gt; inert) and emits
/// <c>_08_Reporting/dice-combo-report.json</c>: per-combo best dice-cycle tier + hop count vs. product
/// reach + cards-in-cycle + AST provenance, plus the engine-DERIVED (novel) dice loops. A soft test of
/// the dice + damage + blink + token arms end to end; reads the committed combos.json + card-inputs.json
/// (no Scryfall fetch).
/// </summary>
public static class DiceComboReportFlow
{
  public static BuiltFlow Create(
    Catalog catalog,
    string ontologyPath,
    string goldFixturesRoot,
    string stubAstsPath
  ) =>
    FlowBuilder.CreateFlow(
      "DiceComboReport",
      pipeline =>
      {
        pipeline.AddStep<IEnumerable<Combo>, IEnumerable<MastCardInput>, Data._08_Reporting.Schemas.DiceComboReport>(
          label: "DiceComboReport",
          transform: DiceComboReportStep.Create(ontologyPath, goldFixturesRoot, stubAstsPath),
          inputs: (catalog.Combos, catalog.CardInputs),
          outputs: catalog.DiceComboReport
        );
      }
    );
}
