using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// One row per unique oracle-text string, paired with its sentence-transformer embedding. This
/// is the persisted encoder cache — <c>EmbedOracleText</c> deduplicates <see cref="OracleLine"/>
/// by <see cref="Text"/>, runs the model only over the unique set, and writes the result here.
/// The 2D / 5D UMAP steps consume <c>OracleLines</c> + this table and join on <see cref="Text"/>
/// to produce per-line vectors.
/// </summary>
/// <remarks>
/// <para>
/// Persisted as parquet — the embedding column is a binary blob (float16 packed little-endian)
/// and a typical run produces ~30K unique strings, so JSON would be unworkable.
/// </para>
/// <para>
/// The vector is packed as a single little-endian <c>byte[]</c> blob (2 bytes per element)
/// rather than a typed <c>float[]</c>. Reason: Flowthru's source-generated <c>IFlatSchema</c>
/// classifier marks any typed array (other than <c>byte[]</c>) as nested, and Parquet requires
/// <c>IFlatSchema</c>. <c>byte[]</c> is the only array form considered flat. Python encodes via
/// <c>vec.astype('&lt;f2').tobytes()</c> and decodes via <c>np.frombuffer(blob, dtype='&lt;f2')</c>.
/// </para>
/// <para>
/// float16 (vs float32) halves the payload, critical for the 768-dim mpnet variant: float32 ×
/// 30K rows hits System.Text.Json's max-value-length on the C# ↔ Python boundary. Precision loss
/// is negligible for normalized embeddings used in similarity / clustering.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record EncodedText
{
  /// <summary>The unique oracle-text string this row encodes. Primary key against
  /// <c>OracleLines.Text</c> on the join.</summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>
  /// Sentence-transformer embedding packed as little-endian float16 bytes
  /// (dim × 2 bytes per row). See remarks on this type for the encoding rationale.
  /// </summary>
  [SerializedLabel("embedding")]
  public required byte[] Embedding { get; init; }
}
