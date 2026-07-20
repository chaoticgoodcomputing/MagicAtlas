namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.CrossTrackJoins.Steps;

/// <summary>
/// The <b>cross-track joins</b> flow (ADR-0004 §4). Two joins, each landing a Derived artifact in
/// <c>Data/_08_Reporting/</c>:
/// <list type="number">
///   <item><c>quarantine-tier-join.json</c> — quarantined oracle text → gold → shipped combo tier.</item>
///   <item><c>guard-witness-join.json</c> — gold <c>declares</c> → rollup rule → engine guard.</item>
/// </list>
///
/// <para><b>Hermetic.</b> Unlike <c>OverApproximation</c> / <c>SpanWitness</c> / <c>TopologyDemand</c>,
/// every input is a committed artifact (quarantine, parse golds, pins, interaction golds, rollup, engine
/// sources), so this flow — and both of its gates — run on a clean checkout with no corpus. A cross-track
/// join that only runs when the corpus happens to be present is a join that silently does not run, which
/// is the failure class §4 was written about.</para>
///
/// <para>Derivation is Flowthru's job; the GATES are the NUnit fixtures under
/// <c>Tests/CrossTrackJoins/</c>, which re-run the same pure <see cref="CrossTrackJoiner"/> rather than
/// reading these (gitignored) outputs.</para>
/// </summary>
public static class CrossTrackJoinsFlow
{
  public static BuiltFlow Create(Catalog catalog, string repoRoot) =>
    FlowBuilder.CreateFlow(
      "CrossTrackJoins",
      pipeline =>
      {
        pipeline.AddStep<Data._08_Reporting.Schemas.QuarantineTierJoin>(
          label: "QuarantineTierJoin",
          transform: QuarantineTierJoinStep.Create(repoRoot),
          outputs: catalog.QuarantineTierJoin
        );

        pipeline.AddStep<Data._08_Reporting.Schemas.GuardWitnessJoin>(
          label: "GuardWitnessJoin",
          transform: GuardWitnessJoinStep.Create(repoRoot),
          outputs: catalog.GuardWitnessJoin
        );
      }
    );
}
