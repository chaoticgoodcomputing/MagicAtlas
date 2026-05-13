using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// One row of the embedding-model training corpus. A row with <see cref="Negative"/> = null is
/// treated as a positive pair (MultipleNegativesRankingLoss; in-batch negatives are inferred); a
/// row with all three fields set is a triplet (TripletLoss). The two losses are mixed in the
/// trainer by sampling rows of each shape.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Source"/> tags which extraction tier the row came from — useful for diagnostic
/// reporting (e.g. "tier-3 triplets contribute 4% of rows but X% of loss") and for ablation
/// runs where one tier is held out.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record TrainingPair
{
  [SerializedLabel("anchor")]
  public required string Anchor { get; init; }

  [SerializedLabel("positive")]
  public required string Positive { get; init; }

  /// <summary>Triplet negative — null for plain positive pairs.</summary>
  [SerializedLabel("negative")]
  public string? Negative { get; init; }

  /// <summary>Sampling weight; default 1.0. Allows boosting tier-3 hard-negative triplets above
  /// the cheaper tier-1/2 positives if their share of the corpus is small but signal is high.</summary>
  [SerializedLabel("weight")]
  public required double Weight { get; init; }

  /// <summary>Extraction provenance — one of <c>"glossary"</c>, <c>"glossary_cr"</c>,
  /// <c>"reminder_text"</c>, <c>"template:&lt;name&gt;"</c>, <c>"curated_definition"</c>,
  /// <c>"curated_triplet"</c>.</summary>
  [SerializedLabel("source")]
  public required string Source { get; init; }
}
