using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.ArtifactCensus.Steps;

/// <summary>
/// Enumerates the declared artifact surface and classifies every file through
/// <see cref="ArtifactClassifier"/>, shaping the result into the reporting manifest. All the judgement
/// lives in the classifier (shared with the NUnit gate); this step is pure projection.
/// </summary>
[FlowthruStep]
public static class CensusStep
{
  public static Func<Data._08_Reporting.Schemas.ArtifactCensus> Create(string repoRoot) =>
    () =>
    {
      var result = ArtifactClassifier.Run(repoRoot);

      static ArtifactEntry Entry(ArtifactClassifier.ArtifactRecord r) =>
        new()
        {
          Path = r.Path,
          Kind = r.Kind,
          Rule = r.Rule,
          Basis = r.Basis,
          Acknowledged = r.Acknowledged,
        };

      static IReadOnlyList<KindCount> Tally<T>(IEnumerable<T> items, Func<T, string> key) =>
        items
          .GroupBy(key, StringComparer.Ordinal)
          .Select(g => new KindCount { Label = g.Key, Count = g.Count() })
          .OrderByDescending(k => k.Count)
          .ThenBy(k => k.Label, StringComparer.Ordinal)
          .ToList();

      // Every kind is reported, including the ones with zero members — a kind that silently vanishes
      // from the manifest is a classification you can no longer see the absence of.
      var kinds = new[]
      {
        ArtifactClassifier.Evidence,
        ArtifactClassifier.Derived,
        ArtifactClassifier.ArchitecturalDecision,
        ArtifactClassifier.NeedsHuman,
      };

      var needsHuman = result
        .Artifacts.Where(a => a.Kind == ArtifactClassifier.NeedsHuman)
        .Select(Entry)
        .ToList();

      return new Data._08_Reporting.Schemas.ArtifactCensus
      {
        GeneratedAt = DateTime.UtcNow,
        ScanRoots = result
          .Roots.Select(r => new ScanRootSummary
          {
            Path = r.Path,
            Rationale = r.Rationale,
            Exists = r.Exists,
            ArtifactCount = r.ArtifactCount,
          })
          .ToList(),
        TotalArtifacts = result.Artifacts.Count,
        ByKind = kinds
          .Select(k => new KindCount
          {
            Label = k,
            Count = result.Artifacts.Count(a => a.Kind == k),
          })
          .ToList(),
        ByRule = Tally(result.Artifacts, a => a.Rule),
        NeedsHumanClassification = needsHuman,
        Unclassified = result.Unclassified.Select(Entry).ToList(),
        Artifacts = result.Artifacts.Select(Entry).ToList(),
        Exclusions = result
          .Exclusions.Select(e => new ExclusionEntry { Path = e.Path, Rule = e.Rule })
          .ToList(),
        ExclusionsByRule = Tally(result.Exclusions, e => e.Rule),
      };
    };
}
