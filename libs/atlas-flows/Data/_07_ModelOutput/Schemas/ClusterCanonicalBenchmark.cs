using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Quality scorecard for a candidate clustering (e.g. HDBSCAN output) measured against the
/// canonical line-attribution ground truth. Produced per-variant by the cluster-vs-canonical
/// benchmark step. One row per cluster, plus one summary row with <c>cluster_id = -2</c>
/// holding the overall corpus-wide metrics.
/// </summary>
/// <remarks>
/// <para>
/// Per-cluster row interpretation:
/// <list type="bullet">
/// <item><c>dominant_canonical</c> — the single canonical that the most member-lines map to.</item>
/// <item><c>purity</c> — fraction of cluster members whose primary canonical == dominant_canonical.
/// 1.0 = perfectly homogeneous; lower = mixed.</item>
/// <item><c>canonical_recall</c> — of all corpus lines mapped to dominant_canonical, what fraction
/// landed in this cluster. 1.0 = this cluster captures the entire canonical.</item>
/// <item><c>n_member_lines</c>, <c>n_canonical_lines</c> — raw counts behind the ratios.</item>
/// </list>
/// </para>
/// <para>
/// Overall row (<c>cluster_id = -2</c>, <c>dominant_canonical = "*"</c>): carries the
/// corpus-level homogeneity / completeness / V-measure / Adjusted Rand Index scores
/// (in the <c>purity</c>, <c>canonical_recall</c>, <c>v_measure</c>, <c>ari</c> fields
/// respectively — overloaded for shape compatibility).
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ClusterCanonicalBenchmark
{
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }

  [SerializedLabel("dominant_canonical")]
  public required string DominantCanonical { get; init; }

  [SerializedLabel("n_member_lines")]
  public required int NMemberLines { get; init; }

  [SerializedLabel("n_canonical_lines")]
  public required int NCanonicalLines { get; init; }

  [SerializedLabel("purity")]
  public required double Purity { get; init; }

  [SerializedLabel("canonical_recall")]
  public required double CanonicalRecall { get; init; }

  /// <summary>V-measure of the cluster (vs. the canonical it best aligns with) — harmonic
  /// mean of homogeneity and completeness. On the overall row this is the corpus-level
  /// V-measure.</summary>
  [SerializedLabel("v_measure")]
  public required double VMeasure { get; init; }

  /// <summary>Adjusted Rand Index — only populated meaningfully on the overall row
  /// (<c>cluster_id = -2</c>); 0 on per-cluster rows.</summary>
  [SerializedLabel("ari")]
  public required double Ari { get; init; }
}
