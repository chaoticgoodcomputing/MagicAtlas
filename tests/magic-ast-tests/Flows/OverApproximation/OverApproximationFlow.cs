namespace MagicAtlas.Ast.Tests.Flows.OverApproximation;

using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.OverApproximation.Steps;

/// <summary>
/// The <b>over-approximation</b> flow (ADR-0004 §6, modeled-dependency completeness). Reads the D1
/// card↔port index (<c>card-ports.json</c> — the card scope plus the Green/Amber tier join) and the card
/// oracle text (<c>card-inputs.json</c>), re-parses each card, and computes <c>AST condition nodes −
/// conditions the projection consumed</c> by ablation. Emits
/// <c>Data/_08_Reporting/over-approximation-report.json</c> (gitignored, never committed).
///
/// <para>Run after a fresh <c>--flow CardAtlas</c> so the ports it joins to are current. A REPORT, never a
/// gate: an accepted over-approximation is legal (ADR-0003 §7) — the requirement §6 imposes is that it be
/// enumerable, so "which GREENs rest on unmodeled conditions" is a query.</para>
/// </summary>
public static class OverApproximationFlow
{
  public static BuiltFlow Create(Catalog catalog, string ontologyPath) =>
    FlowBuilder.CreateFlow(
      "OverApproximation",
      pipeline =>
      {
        pipeline.AddStep<IEnumerable<CardPortRow>, IEnumerable<MastCardInput>, OverApproximationReport>(
          label: "OverApproximation",
          transform: OverApproximationStep.Create(ontologyPath),
          inputs: (catalog.CardPorts, catalog.CardInputs),
          outputs: catalog.OverApproximationReport
        );
      }
    );
}
