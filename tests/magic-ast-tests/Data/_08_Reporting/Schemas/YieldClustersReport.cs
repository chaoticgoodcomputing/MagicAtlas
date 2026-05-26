using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Discovery-side companion to <see cref="TriageReport"/>. Clusters unparsed
/// oracle lines by lexical template (placeholder-substituted oracle text) and
/// projects per-cluster card-flip yield. Greedy set-cover over the clusters
/// produces a recommended batch — the K clusters that jointly flip the most
/// cards green if their parser surfaces are landed together.
/// </summary>
/// <remarks>
/// Unlike <see cref="TriageReport"/>, this report is NOT pattern-name driven.
/// Templates are data-derived from oracle text via tokenization rules, so the
/// long-tail structures that <c>FallbackParser.InferFailurePattern</c> hasn't
/// been taught to recognize still surface here under their template strings.
/// </remarks>
[FlowthruSchema]
public partial record YieldClustersReport
{
  public required string GeneratedAt { get; init; }
  public required int TotalUnparsedCards { get; init; }
  public required int TotalUnparsedLines { get; init; }
  public required int DistinctTemplates { get; init; }

  /// <summary>All clusters sorted by descending direct-yield (cards this single cluster would flip green if closed).</summary>
  public required IReadOnlyList<TemplateCluster> Clusters { get; init; }

  /// <summary>Greedy set-cover output for K=5 batches — the five clusters that jointly flip the most cards.</summary>
  public required IReadOnlyList<BatchRecommendation> RecommendedBatch { get; init; }
}

[FlowthruSchema]
public partial record TemplateCluster
{
  public required int Id { get; init; }

  /// <summary>The placeholder-substituted template. e.g. "Counter target &lt;TYPE&gt; spell."</summary>
  public required string Template { get; init; }

  /// <summary>Number of unparsed lines matching this template.</summary>
  public required int LineCount { get; init; }

  /// <summary>Number of distinct cards with at least one line matching this template.</summary>
  public required int CardCount { get; init; }

  /// <summary>
  /// Cards where this is the ONLY unparsed template — closing this cluster flips
  /// these cards from red to green directly. Upper bound on per-cluster yield.
  /// </summary>
  public required int DirectYield { get; init; }

  /// <summary>
  /// Cards where this cluster appears alongside others — closing only this one
  /// shrinks the gap but doesn't flip the card. Joint yield with co-occurring
  /// clusters is captured in <see cref="RecommendedBatch"/>.
  /// </summary>
  public required int PartialYield { get; init; }

  /// <summary>
  /// Best fixture candidates — cards with the FEWEST other unparsed clusters
  /// (cleanest exemplars), then by oracle-line length ascending. Capped at 8.
  /// </summary>
  public required IReadOnlyList<ExemplarLine> ExemplarLines { get; init; }

  /// <summary>Top 5 cluster IDs that co-occur on cards with this one (Jaccard-sorted).</summary>
  public required IReadOnlyList<int> CoOccurringClusters { get; init; }
}

[FlowthruSchema]
public partial record ExemplarLine
{
  public required string CardName { get; init; }
  public required string ScryfallId { get; init; }
  public required string OracleLine { get; init; }
  public required int OtherUnparsedClusters { get; init; }
}

[FlowthruSchema]
public partial record BatchRecommendation
{
  public required int Rank { get; init; }
  public required int ClusterId { get; init; }
  public required string Template { get; init; }

  /// <summary>New cards this cluster flips, given the prior picks already committed.</summary>
  public required int MarginalYield { get; init; }

  /// <summary>Cumulative cards flipped through this pick.</summary>
  public required int CumulativeYield { get; init; }
}
