namespace MagicAtlas.Ast.Tests.Flows.WidenedAttributes;

using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.WidenedAttributes.Steps;

/// <summary>
/// The <b>widened-attribute</b> flow (ADR-0004 §6). Reads the D1 card↔port index
/// (<c>card-ports.json</c> — the card scope plus the Green/Amber tier join) and the card oracle text
/// (<c>card-inputs.json</c>), re-parses each card, and computes <c>AST facets − facets the projection
/// consumed</c> by ablation. Emits <c>Data/_08_Reporting/widened-attribute-report.json</c> (gitignored,
/// never committed).
///
/// <para>Sibling of the <c>OverApproximation</c> flow, never a substitute for it: that one enumerates
/// dropped condition NODES (a lost guard), this one enumerates dropped FACETS (a lost scope). The two
/// partition the AST structurally — see <see cref="WidenedAttributeReport"/>.</para>
///
/// <para>Run after a fresh <c>--flow CardAtlas</c> so the ports it joins to are current. A REPORT, never
/// a gate: it is a burn-down list, and the fix for a row is to carry the facet through the projection.</para>
/// </summary>
public static class WidenedAttributesFlow
{
  public static BuiltFlow Create(Catalog catalog, string ontologyPath) =>
    FlowBuilder.CreateFlow(
      "WidenedAttributes",
      pipeline =>
      {
        pipeline.AddStep<IEnumerable<CardPortRow>, IEnumerable<MastCardInput>, WidenedAttributeReport>(
          label: "WidenedAttributes",
          transform: WidenedAttributesStep.Create(ontologyPath),
          inputs: (catalog.CardPorts, catalog.CardInputs),
          outputs: catalog.WidenedAttributeReport
        );
      }
    );
}
