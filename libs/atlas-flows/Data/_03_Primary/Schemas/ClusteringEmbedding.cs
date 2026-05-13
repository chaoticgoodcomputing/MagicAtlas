using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A single oracle-text fragment's 5D UMAP-reduced embedding — the shared intermediate between
/// the Clustering flow's HDBSCAN step and the ModelEvaluations flow's centroid-distance metric.
/// Materializing the 5D reduction (rather than computing it inside the clusterer) means
/// evaluations can A/B between embedding-model variants without redundant UMAP work, and
/// HDBSCAN noise-handling can be retuned without re-running UMAP.
/// </summary>
/// <remarks>
/// <para>
/// Persisted as parquet for the same reason as <see cref="BertEmbedding"/> — JSON would balloon
/// at ~50K rows × 5 floats.
/// </para>
/// <para>
/// The 5-dim float32 vector is packed as a single little-endian <c>byte[]</c> blob (20 bytes per
/// row) for the same flat-schema reason documented on <see cref="BertEmbedding"/>: Flowthru's
/// source-generated <c>IFlatSchema</c> classifier rejects typed <c>float[]</c>, and
/// <c>byte[]</c> is the only array form considered flat. Python encodes via
/// <c>vec.astype('&lt;f4').tobytes()</c> and decodes via <c>np.frombuffer(blob, dtype='&lt;f4')</c>.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ClusteringEmbedding
{
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  /// <summary>
  /// 5-dim float32 UMAP coordinates packed as little-endian bytes (20 bytes per row).
  /// </summary>
  [SerializedLabel("vector")]
  public required byte[] Vector { get; init; }
}
