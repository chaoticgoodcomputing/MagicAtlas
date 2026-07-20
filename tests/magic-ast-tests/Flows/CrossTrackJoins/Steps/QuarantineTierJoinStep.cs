namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins.Steps;

using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// ADR-0004 §4, join 1 — <c>quarantined-oracle-text → gold → shipped combo tier</c>. Loads both tracks'
/// committed artifacts and shapes <see cref="CrossTrackJoiner.JoinQuarantineToTiers"/>'s result into the
/// reporting artifact. All the judgement lives in the joiner (shared with the NUnit gate); this step is
/// pure projection, exactly as <c>CensusStep</c> is for the artifact census.
/// </summary>
[FlowthruStep]
public static class QuarantineTierJoinStep
{
  public static Func<Data._08_Reporting.Schemas.QuarantineTierJoin> Create(string repoRoot) =>
    () =>
    {
      var result = CrossTrackJoiner.JoinQuarantineToTiers(
        CrossTrackSources.LoadQuarantine(repoRoot),
        CrossTrackSources.LoadCardByFixture(repoRoot),
        CrossTrackSources.LoadPins(repoRoot),
        CrossTrackSources.LoadInteractionGoldsByCard(repoRoot),
        CrossTrackSources.LoadAcknowledged(repoRoot)
      );

      return new Data._08_Reporting.Schemas.QuarantineTierJoin
      {
        GeneratedAt = DateTime.UtcNow.ToString("O"),
        Note =
          "ADR-0004 §4 join 1. A GREEN pin resting on quarantined oracle text is the Suture Priest shape: "
          + "the quarantine entry is correct and current, it is simply a Parse-track fact with no edge to "
          + "Interaction-track tiering. Acknowledged (comboId, fixture) pairs come from "
          + "tools/bench/MagicAtlas.Bench/fidelity-risk-acknowledged.json — reused, not duplicated.",
        QuarantinedFixtureCount = result.QuarantinedFixtureCount,
        ResolvedFixtureCount = result.ResolvedFixtureCount,
        PinCount = result.PinCount,
        GreenPinCount = result.GreenPinCount,
        Vacuous = result.Vacuous,
        ViolationCount = result.Violations.Count,
        UnresolvedFixtures = result.UnresolvedFixtures,
        Risks =
        [
          .. result
            .Risks.OrderByDescending(r => r.IsViolation)
            .ThenBy(r => r.ComboId, StringComparer.Ordinal)
            .ThenBy(r => r.Fixture, StringComparer.Ordinal)
            .Select(r => new QuarantineRiskRow
            {
              ComboId = r.ComboId,
              Tier = r.Tier,
              Card = r.Card,
              Fixture = r.Fixture,
              Tag = r.Tag,
              Reason = r.Reason,
              Acknowledged = r.Acknowledged,
              Violation = r.IsViolation,
              InteractionGolds = r.InteractionGolds,
            }),
        ],
      };
    };
}
