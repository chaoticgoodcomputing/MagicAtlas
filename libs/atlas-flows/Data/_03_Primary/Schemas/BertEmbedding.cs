using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A single ability-fragment BERT embedding — the shared intermediate between the OracleEmbedding
/// flow's 2D-UMAP reduction (for display) and the Clustering flow's 5D-UMAP+HDBSCAN reduction
/// (for cluster discovery). One row per fragment; the BERT encode pass is materialized so it runs
/// once and both downstream reductions feed from the same vectors.
/// </summary>
/// <remarks>
/// <para>
/// Persisted as parquet — JSON would be hundreds of MB and slow to (de)serialize.
/// </para>
/// <para>
/// The 384-dim float32 vector is packed as a single little-endian <c>byte[]</c> blob (1,536 bytes
/// per row) rather than a typed <c>float[]</c>. Reason: Flowthru's source-generated
/// <c>IFlatSchema</c> classifier marks any typed array (other than <c>byte[]</c>) as nested, and
/// <c>Parquet</c> requires <c>IFlatSchema</c>. <c>byte[]</c> is the only array form Flowthru
/// considers flat (Tier 3, opaque blob). Python encodes via <c>vec.astype('&lt;f4').tobytes()</c>
/// and decodes via <c>np.frombuffer(blob, dtype='&lt;f4')</c>.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record BertEmbedding
{
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>Carried through from <see cref="OracleInput"/> so downstream consumers don't need to
  /// rejoin against the text fragments to know which classification this vector came from.</summary>
  [SerializedLabel("text_type")]
  public required string TextType { get; init; }

  /// <summary>
  /// 384-dim float32 sentence-transformer embedding packed as little-endian bytes
  /// (1,536 bytes per row). See remarks on this type for the reasoning.
  /// </summary>
  [SerializedLabel("embedding")]
  public required byte[] Embedding { get; init; }
}
