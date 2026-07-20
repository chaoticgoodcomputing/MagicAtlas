namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins.Steps;

using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// ADR-0004 §4, join 2 — <c>gold <c>declares</c> → rollup rule → engine guard</c>. Materializes the
/// guard→witness map and the three consistency deltas around it.
///
/// <para><b>Note the derivation order, which is the point.</b> The map is built from the golds' declares
/// FIRST; the rollup and the engine-source scan are joined onto it afterwards. Nothing in this step, the
/// joiner, or the sources contains a rule id, so there is no table to keep true.</para>
/// </summary>
[FlowthruStep]
public static class GuardWitnessJoinStep
{
  public static Func<Data._08_Reporting.Schemas.GuardWitnessJoin> Create(string repoRoot) =>
    () =>
    {
      var (declared, citations, goldsScanned) = CrossTrackSources.LoadGoldDeclarations(repoRoot);
      var rollup = CrossTrackSources.LoadRollupRules(repoRoot);
      var (codeRefs, filesScanned) = CrossTrackSources.ScanEngineSources(
        repoRoot,
        declared.Select(d => d.RuleId).Concat(rollup.Select(r => r.RuleId))
      );

      var result = CrossTrackJoiner.JoinGuardsToWitnesses(
        declared,
        citations,
        rollup,
        codeRefs,
        goldsScanned,
        filesScanned
      );

      return new Data._08_Reporting.Schemas.GuardWitnessJoin
      {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        Note =
          "ADR-0004 §4 join 2 / §2 soundness half. witnesses(rule) is GROUPED OUT OF the golds' declares "
          + "blocks — zero hand-authored entries, no registry. codeReferences is a literal scan of libs/**/*.cs "
          + "for each rule id: the rollup-rule → engine-guard leg, reported (not gated) because closing it "
          + "is issue #34's job.",
        GoldsScanned = result.GoldsScanned,
        GoldsDeclaringRules = result.GoldsDeclaringRules,
        RuleCount = result.Rules.Count,
        RollupRuleCount = result.RollupRuleCount,
        SourceFilesScanned = result.SourceFilesScanned,
        Vacuous = result.Vacuous,
        UnwitnessedRuleCount = result.Unwitnessed.Count,
        CodeUnlinkedRuleCount = result.CodeUnlinked.Count,
        WitnessDisagreements =
        [
          .. result.Disagreements.Select(d => new WitnessDisagreementRow
          {
            RuleId = d.RuleId,
            InGoldsOnly = d.InGoldsOnly,
            InRollupOnly = d.InRollupOnly,
          }),
        ],
        RollupRulesMissingFromGolds = result.RollupRulesMissingFromGolds,
        GoldRulesMissingFromRollup = result.GoldRulesMissingFromRollup,
        DanglingCitations = [.. result.DanglingCitations.Select(c => $"{c.GoldId}#{c.EdgeId} → {c.RuleId}")],
        Rules =
        [
          .. result.Rules.Select(r => new GuardWitnessRow
          {
            RuleId = r.RuleId,
            Section = r.Section,
            Impl = r.Impl,
            Status = r.Status,
            Witnesses = r.Witnesses,
            CitingGolds = r.CitingGolds,
            CodeReferences = [.. r.CodeReferences.Select(x => $"{x.Path}:{x.Line}")],
            Desc = r.Desc,
            Cr = r.Cr,
          }),
        ],
      };
    };
}
