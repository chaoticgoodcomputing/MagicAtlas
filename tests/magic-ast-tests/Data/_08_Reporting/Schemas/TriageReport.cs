using Flowthru.Data.Schema;
using MagicAST;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Top-level triage report consumed by the <c>mast-tdd-loop</c> skill. Ranks
/// failure patterns by projected coverage gain and surfaces clean exemplars for
/// each. The on-disk JSON is PascalCase — Flowthru's default — and the
/// <c>mast-tdd-loop</c> skill is the consuming contract.
/// </summary>
[FlowthruSchema]
public partial record TriageReport
{
  public required DateTime GeneratedAt { get; init; }
  public required GlobalMetrics GlobalMetrics { get; init; }
  public required IReadOnlyList<GapEntry> TopGaps { get; init; }
}

/// <summary>Coverage and pattern-frequency metrics aggregated across the corpus.</summary>
[FlowthruSchema]
public partial record GlobalMetrics
{
  public required CoverageStat CardCoverage { get; init; }
  public required CoverageStat LineCoverage { get; init; }
  public required int DistinctUnresolvedPatterns { get; init; }
  public required CoverageStat HandParsedCoverage { get; init; }
}

/// <summary>A passing-out-of-total ratio plus its percent form.</summary>
[FlowthruSchema]
public partial record CoverageStat
{
  public required int Passing { get; init; }
  public required int Total { get; init; }
  public required double Pct { get; init; }
}

/// <summary>One row in the top-gaps ranking — a failure pattern and its actionable exemplars.</summary>
[FlowthruSchema]
public partial record GapEntry
{
  public required int Rank { get; init; }
  public required string Pattern { get; init; }
  public required GapFrequency Frequency { get; init; }
  public required CoverageGain ProjectedCoverageGain { get; init; }

  /// <summary>
  /// Patterns that frequently co-occur on the same lines as this one
  /// (per-line Jaccard ≥ threshold). The orchestrator should avoid
  /// dispatching paralleled sub-agents on related patterns.
  /// </summary>
  public required IReadOnlyList<string> RelatedPatterns { get; init; }

  /// <summary>Clean exemplar lines, ranked by cleanliness ascending. Capped at ~10.</summary>
  public required IReadOnlyList<CandidateLine> CandidateLines { get; init; }
}

/// <summary>How many lines and how many distinct cards exhibit this pattern.</summary>
[FlowthruSchema]
public partial record GapFrequency
{
  public required int Lines { get; init; }
  public required int Cards { get; init; }
}

/// <summary>Projected percentage-point gain in card- and line-level coverage.</summary>
[FlowthruSchema]
public partial record CoverageGain
{
  public required double CardCoveragePct { get; init; }
  public required double LineCoveragePct { get; init; }
}

/// <summary>
/// A candidate oracle line surfaced for hand-parsing. Lower
/// <see cref="CleanlinessScore"/> means the line's failures are dominated by
/// the parent <c>GapEntry.Pattern</c> (Definition D: 1 - P-purity).
/// </summary>
[FlowthruSchema]
public partial record CandidateLine
{
  public required string OracleText { get; init; }
  public required CandidateLineSource SourceCard { get; init; }
  public required double CleanlinessScore { get; init; }
  public required int LineLength { get; init; }

  /// <summary>True if this card already has a hand-parsed fixture under <c>HandParsedCards/</c>.</summary>
  public required bool AlreadyHandParsed { get; init; }
}

/// <summary>Source-card pointer attached to each candidate line.</summary>
[FlowthruSchema]
public partial record CandidateLineSource
{
  public required string Name { get; init; }
  public required string ScryfallId { get; init; }

  /// <summary>The DTO an agent can hand-parse directly without re-fetching from Scryfall.</summary>
  public required CardInputDTO Input { get; init; }
}
