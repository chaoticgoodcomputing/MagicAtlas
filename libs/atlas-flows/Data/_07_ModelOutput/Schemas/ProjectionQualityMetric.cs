using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Cross-level projection-quality scorecard: HD (raw embedding) vs 5D (clustering UMAP) vs 2D
/// (atlas UMAP), measured against two user camps — Exploration (does the projection reveal
/// canonical structure?) and Exploitation (does the projection preserve HD's local neighborhoods?).
/// Tidy long form: one row per (level, camp, metric). Render as a 3×8 grid for the report.
/// </summary>
/// <remarks>
/// <para>Levels (<see cref="Level"/>): <c>"hd"</c>, <c>"5d"</c>, <c>"2d"</c>.</para>
/// <para>Camps (<see cref="Camp"/>): <c>"exploration"</c>, <c>"exploitation"</c>.</para>
/// <para>
/// Exploration metrics (absolute structural quality; HD acts as the achievable ceiling):
/// <list type="bullet">
///   <item><c>silhouette_leaf</c> — centroid-based silhouette using full canonical slugs.</item>
///   <item><c>silhouette_parent</c> — same, using only the parent prefix (before first colon).
///     Asymmetry vs leaf reveals whether coarse hierarchy survives even when 347 leaves collapse.</item>
///   <item><c>pairwise_centroid_spearman</c> — Spearman correlation of all canonical-pair centroid
///     distances between this level and HD. 1.0 at HD by definition; measures whether
///     "semantically close canonicals stay visually close" — the navigability metric.</item>
///   <item><c>nmi_kmeans</c> — Normalized Mutual Information between canonical labels and
///     unsupervised kmeans (k = number of canonicals) on the projection. Tests whether the
///     projection encoded canonical structure intrinsically (independent of supervision).</item>
/// </list>
/// </para>
/// <para>
/// Exploitation metrics (preservation of HD geometry; 1.0 at HD by definition):
/// <list type="bullet">
///   <item><c>trustworthiness_k10</c> — fraction of a point's 2D K-NN that were also K-NN in HD.
///     Penalizes "spurious" near-neighbors in the projection.</item>
///   <item><c>continuity_k10</c> — inverse: fraction of HD K-NN preserved in the projection.
///     Penalizes "lost" near-neighbors — the budget alternative going missing.</item>
///   <item><c>card_jaccard_k10</c> — for each card (line embeddings mean-pooled), Jaccard
///     overlap between top-10 nearest cards in HD vs the projection, averaged across cards.
///     The user-facing "click my card → are budget alternatives nearby" metric.</item>
///   <item><c>knn_purity_k10</c> — for each line, fraction of 10 nearest neighbors sharing the
///     line's canonical. Local-neighborhood analog of silhouette; what users actually experience
///     when hovering.</item>
/// </list>
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ProjectionQualityMetric
{
  [SerializedLabel("level")]
  public required string Level { get; init; }

  [SerializedLabel("camp")]
  public required string Camp { get; init; }

  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  [SerializedLabel("value")]
  public required double Value { get; init; }
}
