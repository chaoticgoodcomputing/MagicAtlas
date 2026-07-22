namespace MagicAtlas.Ast.Tests.Flows.DerivedBacklog.Steps;

using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Assembles ADR-0004 §2's derived backlog from the three derived terms and writes
/// <c>Data/_08_Reporting/derived-backlog.json</c>. Hermetic: reflects the loaded MagicAST/engine assemblies
/// for <c>projected</c>/<c>served</c> and reads the committed golds + reconstruction pins for the subtrahend
/// and the combo-level demand. All the judgement lives in the golds; this step is projection.
/// </summary>
[FlowthruStep]
public static class DerivedBacklogStep
{
  private const string Note =
    "ADR-0004 §2 derived backlog — computed, never stored. "
    + "backlog = projected(corpus) − served(rollup ∪ guards) − asserted-unarmable(golds). "
    + "projected(corpus) = every EffectType/CostType/trigger Event/restriction the PortWalk dispatch can "
    + "produce a port from (the projectable universe, reflected from the AST schema + enums; "
    + "corpus-INDEPENDENT, so the size is deterministic on a corpus-less checkout). "
    + "served = PortWalkProjection (reflected from engine code, not a stored list). "
    + "asserted-unarmable = the interaction golds' no_arm assertions over projected ports, derived live. "
    + "An unserved projection with no gold is BACKLOG; with an asserted-absence gold it is a DECISION. "
    + "Retires holes{} (#26) and known-coarse-projections.json (#32). The GATE is PortWalkExhaustivenessTests, "
    + "which re-runs BacklogDerivation.Compute over the live schema/engine/golds rather than reading this file.";

  public static Func<DerivedBacklog> Create(string repoRoot) =>
    () =>
    {
      var all = BacklogDerivation.AllByDimension();
      var served = BacklogDerivation.ServedByDimension();
      var decisionSources = BacklogSources.LoadAssertedUnarmable(BacklogSources.GoldsDir(repoRoot), all);
      var assertedUnarmable = decisionSources.Select(d => d.Term).ToHashSet();

      var result = BacklogDerivation.Compute(all, served, assertedUnarmable, BacklogDerivation.NotPortCandidates);

      var goldByTerm = decisionSources.ToDictionary(d => d.Term, d => d);

      var dimensions = result
        .ByDimension.OrderBy(kv => kv.Key, StringComparer.Ordinal)
        .Select(kv => new BacklogDimension
        {
          Dimension = kv.Key,
          All = kv.Value.All,
          Served = kv.Value.Served,
          BacklogCount = kv.Value.Backlog,
          Backlog = result
            .Backlog.Where(t => t.Dimension == kv.Key)
            .Select(t => t.Discriminator)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList(),
        })
        .ToList();

      BacklogDecision ToDecision(BacklogDerivation.Term t) =>
        new()
        {
          Dimension = t.Dimension,
          Discriminator = t.Discriminator,
          Gold = goldByTerm.TryGetValue(t, out var s) ? s.Gold : "(unknown)",
          Claim = goldByTerm.TryGetValue(t, out var s2) ? s2.Claim : "",
        };

      // ── attribute-axis backlog (declared − witnessed) ──
      var witnessedAxes = BacklogSources.WitnessedAttributeAxes(BacklogSources.GoldsDir(repoRoot));
      var axisBacklog = BacklogSources.DeclaredUnwitnessedAxes
        .Where(a => !witnessedAxes.Contains(a, StringComparer.Ordinal))
        .OrderBy(a => a, StringComparer.Ordinal)
        .ToList();

      // ── combo-level unserved demand ──
      var (combosAvailable, combos) = BacklogSources.LoadUnreconstructedCombos(repoRoot);

      Console.Error.WriteLine(
        $"[DerivedBacklog] {result.Backlog.Count} backlog discriminators, {result.Decisions.Count} decisions, "
          + $"{result.Excluded.Count} excluded (not-a-port-candidate), {result.DanglingDecisions.Count} dangling; "
          + $"{axisBacklog.Count} axis-backlog, {combos.Count} unreconstructed combos"
          + (combosAvailable ? "" : " (combo pins absent)")
      );

      return new DerivedBacklog
      {
        GeneratedAt = DateTime.UtcNow,
        Note = Note,
        TotalBacklog = result.Backlog.Count,
        Dimensions = dimensions,
        Decisions = result.Decisions.Select(ToDecision).ToList(),
        ExcludedNotPortCandidates = result
          .Excluded.Select(t => new BacklogExclusion
          {
            Dimension = t.Dimension,
            Discriminator = t.Discriminator,
            Reason = BacklogDerivation.NotPortCandidateReason,
          })
          .ToList(),
        DanglingDecisions = result.DanglingDecisions.Select(ToDecision).ToList(),
        AttributeAxes = new AttributeAxisBacklog
        {
          Note =
            "Declared attribute axes no gold witnesses (ADR-0003 §4a.1). 'owner' is declared-but-unwitnessed "
            + "backlog and leaves this list the moment a gold's port carries an owner attr. witnessed = the "
            + "union of every gold port's attrs keys, derived live.",
          Witnessed = witnessedAxes,
          Backlog = axisBacklog,
        },
        Combos = new ComboBacklog
        {
          Note =
            "Combo-level unserved demand (a different granularity from the discriminator backlog): combos the "
            + "engine reconstructs no spanning cycle over, from the reconstruction pins' unreconstructed "
            + "section (#31a's '#32 inheritance'). Read-only over the committed bench pins; the discriminator "
            + "backlog above needs no corpus, this section needs only the committed pins.",
          Available = combosAvailable,
          Unreconstructed = combos,
        },
      };
    };
}
