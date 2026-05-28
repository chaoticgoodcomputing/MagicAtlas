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

  /// <summary>
  /// Discovery-side recommendation: the K clusters greedy set-cover picks as
  /// the highest-yield batch. Data-derived from oracle-text lexical templates,
  /// independent of the hand-coded <c>FallbackParser.InferFailurePattern</c>
  /// taxonomy. Orchestrator should weigh BOTH this list and <see cref="TopGaps"/>
  /// when choosing the next batch's families.
  /// </summary>
  public required IReadOnlyList<YieldClusterSummary> TopYieldClusters { get; init; }

  public required IReadOnlyList<GapEntry> TopGaps { get; init; }

  /// <summary>
  /// Same gap entries as TopGaps but ranked by raw line frequency (Frequency.Lines)
  /// descending. Surfaces the highest-frequency parser bail points regardless of
  /// whether they exclusively flip whole cards. Complements TopGaps' card-yield
  /// ranking with a parser-surface-improvement perspective.
  /// </summary>
  public required IReadOnlyList<GapEntry> TopGapsByLineFrequency { get; init; }
}

/// <summary>
/// One cluster in the yield-projection top-K. Templates are placeholder-
/// substituted oracle text (e.g., <c>"Counter target &lt;TYPE&gt; spell."</c>).
/// </summary>
[FlowthruSchema]
public partial record YieldClusterSummary
{
  /// <summary>Position in the greedy set-cover ranking (1-indexed).</summary>
  public required int Rank { get; init; }

  /// <summary>Placeholder-substituted oracle template that defines this cluster.</summary>
  public required string Template { get; init; }

  /// <summary>Total unparsed lines matching this template across the corpus.</summary>
  public required int LineCount { get; init; }

  /// <summary>Number of distinct cards with at least one line matching this template.</summary>
  public required int CardCount { get; init; }

  /// <summary>
  /// Cards whose ENTIRE unparsed-line set is this single template — closing
  /// this cluster would flip them green directly. Upper bound on the cluster's
  /// standalone yield.
  /// </summary>
  public required int DirectYield { get; init; }

  /// <summary>
  /// Cards this cluster flips when picked at <see cref="Rank"/>, given the
  /// prior picks already committed. Greedy-set-cover marginal contribution.
  /// </summary>
  public required int MarginalYield { get; init; }

  /// <summary>Running total of <see cref="MarginalYield"/> through this rank.</summary>
  public required int CumulativeYield { get; init; }

  /// <summary>
  /// Best fixture candidates — cards with the FEWEST other unparsed templates
  /// (cleanest exemplars). Capped at 5.
  /// </summary>
  public required IReadOnlyList<YieldExemplar> Exemplars { get; init; }
}

/// <summary>A specific oracle line that exemplifies a yield cluster's template.</summary>
[FlowthruSchema]
public partial record YieldExemplar
{
  public required string CardName { get; init; }
  public required string ScryfallId { get; init; }
  public required string OracleLine { get; init; }

  /// <summary>How many OTHER unparsed templates this card has (lower = cleaner fixture).</summary>
  public required int OtherUnparsedClusters { get; init; }
}

/// <summary>Coverage and pattern-frequency metrics aggregated across the corpus.</summary>
[FlowthruSchema]
public partial record GlobalMetrics
{
  public required CoverageStat CardCoverage { get; init; }
  public required CoverageStat LineCoverage { get; init; }
  public required CoverageStat AbilityCoverage { get; init; }
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

  /// <summary>
  /// Parser-rule near-miss identifier — the <c>Diagnostic.LastAttemptedRule</c>
  /// value shared by this gap's failures. Combined with <see cref="Pattern"/>,
  /// the pair is the unique key for a gap entry: grouping by
  /// <c>(Pattern, LastAttemptedRule)</c> distinguishes e.g. a "ConditionalEffect"
  /// pattern arriving via the spell dispatch chain from the same pattern
  /// arriving via the triggered dispatch chain. Null only when the entry was
  /// produced before the telemetry wiring landed (legacy diagnostics).
  /// </summary>
  public string? LastAttemptedRule { get; init; }

  /// <summary>
  /// The mode (most-common) <c>FailurePosition</c> across the failures in this
  /// group. The mode is preferred over the median because failure positions
  /// tend to cluster on specific offsets (e.g. clause-start of an offending
  /// sub-rule), and the mode preserves the cluster rather than averaging it
  /// away. Null when no failures in the group carry a position.
  /// </summary>
  public int? ModeFailurePosition { get; init; }

  public required GapFrequency Frequency { get; init; }
  public required CoverageGain ProjectedCoverageGain { get; init; }

  /// <summary>
  /// Proximity-weighted yield: the sum, over every card touching this gap, of
  /// <c>1 / (distinct gap-keys on that card)</c>. A card one gap away from
  /// completion contributes 1.0, two-away 0.5, three-away 0.33, etc. This
  /// generalises the binary "exclusive card" count
  /// (<see cref="CoverageGain.CardCoveragePct"/>) into a continuous signal:
  /// gaps that are the last-or-nearly-last missing piece on many cards rank
  /// highest. It is the primary ranking key for <c>TopGaps</c> — a factor in
  /// the ranking, not a hard one-away filter.
  /// </summary>
  public required double FractionalYield { get; init; }

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
