using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Tidy-long-form per-archetype quality scorecard. Two row shapes share the table:
/// per-archetype metrics (slug = canonical slug) and per-pair metrics (slug = "slug_a|slug_b").
/// </summary>
/// <remarks>
/// Per-archetype metrics (one row each):
/// <list type="bullet">
///   <item><c>n_prototypes</c> — count of prototype clauses authored for this archetype.</item>
///   <item><c>intra_coherence</c> — mean pairwise cosine of this archetype's prototype embeddings.
///     Healthy: 0.7+ (prototypes cluster tightly). Loose: &lt;0.6 (prototypes span too broad a
///     region; centroid is diffuse).</item>
///   <item><c>n_attributions</c> — how many lines were attributed to this archetype as
///     primary canonical.</item>
///   <item><c>mean_confidence</c> — mean cosine confidence of prototype-attribution rows for
///     this archetype.</item>
/// </list>
///
/// Per-pair metrics (one row per archetype pair):
/// <list type="bullet">
///   <item><c>centroid_pair_cosine</c> — cosine between the two archetypes' prototype centroids.
///     High (&gt;0.85): archetypes are at risk of confusion. Healthy: &lt;0.7.</item>
/// </list>
/// </remarks>
[FlowthruSchema]
public partial record ArchetypeQualityMetric
{
  /// <summary>Either a canonical slug, or <c>"slug_a|slug_b"</c> for per-pair metrics.</summary>
  [SerializedLabel("slug")]
  public required string Slug { get; init; }

  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  [SerializedLabel("value")]
  public required double Value { get; init; }
}
