using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.Common;

namespace MagicAtlas.Ast.Tests.Flows.DiscriminatorGovernance.Steps;

/// <summary>
/// Reflects the discriminator vocabulary, computes the intra-family near-duplicate pairs, and joins each
/// to the declaration-site ruling that explains it. All the judgement lives in the attributes; this step
/// is projection.
/// </summary>
[FlowthruStep]
public static class GovernanceStep
{
  public static Func<Data._08_Reporting.Schemas.DiscriminatorGovernance> Create() =>
    () =>
    {
      var declared = DiscriminatorNearness.All();
      var pairs = DiscriminatorNearness.NearPairs(declared);

      return new Data._08_Reporting.Schemas.DiscriminatorGovernance
      {
        GeneratedAt = DateTime.UtcNow,
        Discriminators = declared.Count,
        NearDuplicatePairs = pairs.Count,
        UnexplainedPairs = pairs.Count(p => p.ExplainedBy is null),
        DeadRulings = DiscriminatorNearness.DeadRulings(declared),
        RulingsWithoutReason = declared
          .Where(d => d.NearDuplicateOf.Count > 0 && string.IsNullOrWhiteSpace(d.Reason))
          .Select(d => $"[{d.Family}] {d.TypeName} (\"{d.Value}\")")
          .OrderBy(s => s, StringComparer.Ordinal)
          .ToList(),
        Pairs = pairs
          .Select(p => new NearDuplicatePair
          {
            Family = p.Family,
            A = p.A,
            B = p.B,
            Nearness = p.Nearness,
            ExplainedBy = p.ExplainedBy,
            Reason = p.Reason,
          })
          .ToList(),
      };
    };
}
