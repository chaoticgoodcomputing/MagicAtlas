using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.DerivedBacklog.Steps;

namespace MagicAtlas.Ast.Tests.Flows.DerivedBacklog;

/// <summary>
/// The ADR-0004 §2 derived backlog → <c>Data/_08_Reporting/derived-backlog.json</c> (issue #32).
/// <c>backlog = projected(corpus) − served(rollup ∪ guards) − asserted-unarmable(golds)</c>, all three
/// terms derived — nothing stored. Replaces the retired <c>holes{}</c> registry (#26) and the deleted
/// <c>known-coarse-projections.json</c> whitelist.
///
/// <para><b>Hermetic:</b> reflects the loaded MagicAST/engine assemblies for <c>projected</c>/<c>served</c>
/// and reads only committed artifacts (the interaction golds, the reconstruction pins) for the subtrahend
/// and the combo-level demand, so the flow — and its gate — run on a clean checkout with no corpus. Like the
/// sibling cross-track joins: a backlog that only computes when the corpus happens to be present is a
/// backlog that silently does not compute.</para>
///
/// <para>Derivation is Flowthru's job; the GATE is the NUnit <c>PortWalkExhaustivenessTests</c>, which
/// re-runs the same pure <see cref="BacklogDerivation.Compute"/> rather than reading this (gitignored)
/// output.</para>
/// </summary>
public static class DerivedBacklogFlow
{
  public static BuiltFlow Create(Catalog catalog, string repoRoot) =>
    FlowBuilder.CreateFlow(
      "DerivedBacklog",
      pipeline =>
      {
        pipeline.AddStep<Data._08_Reporting.Schemas.DerivedBacklog>(
          label: "DerivedBacklog",
          transform: DerivedBacklogStep.Create(repoRoot),
          outputs: catalog.DerivedBacklog
        );
      }
    );
}
