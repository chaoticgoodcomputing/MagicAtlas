using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.ArtifactCensus.Steps;

namespace MagicAtlas.Ast.Tests.Flows.ArtifactCensus;

/// <summary>
/// The <b>artifact census</b> (ADR 0004 §1, issue #21) — enumerates every artifact under the declared
/// surface (<c>tests/**/Fixtures</c>, <c>**/Data/_08_Reporting</c>, <c>dumps/</c>, <c>libs/**/*.json</c>,
/// plus the committed snapshot families the census itself surfaced) and classifies each as
/// <b>Evidence</b>, <b>Derived</b> or <b>architectural-decision</b>, flagging the genuinely ambiguous
/// residue for human classification rather than guessing → <c>Data/_08_Reporting/artifact-census.json</c>.
/// </summary>
/// <remarks>
/// <para>The census is a Flowthru flow because ADR 0004 §1 says so explicitly: "derivation is Flowthru's
/// job; NUnit's job is gates … a derivation that lives anywhere else is itself a hand-rolled artifact in
/// disguise." The gate over the classification is
/// <c>Tests/ArtifactCensus/ArtifactClassificationGateTests.cs</c>.</para>
/// <para>Hermetic: reads only the working tree (no corpus, no network), so it runs on a clean checkout.</para>
/// </remarks>
public static class ArtifactCensusFlow
{
  public static BuiltFlow Create(Catalog catalog, string repoRoot) =>
    FlowBuilder.CreateFlow(
      "ArtifactCensus",
      pipeline =>
      {
        pipeline.AddStep<Data._08_Reporting.Schemas.ArtifactCensus>(
          label: "ClassifyArtifacts",
          transform: CensusStep.Create(repoRoot),
          outputs: catalog.ArtifactCensus
        );
      }
    );
}
