using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the OracleEmbedding flow — sentence-transformer batch size and the
/// 2D-target UMAP knobs used by <c>reduce_to_2d.py</c>. Materialized at startup from
/// <c>Flowthru:Flows:OracleEmbedding</c> in <c>appsettings.json</c>. All fields are flat
/// scalars (Flowthru's Python marshaller can't encode nested POCOs as step inputs).
/// </summary>
[FlowthruSchema]
public partial record OracleEmbeddingConfig
{
  /// <summary>Per-call batch size for <c>SentenceTransformer.encode</c>.</summary>
  public int EmbedBatchSize { get; init; }

  /// <summary>UMAP <c>n_neighbors</c> for the 2D atlas-display reduction.</summary>
  public int Umap2DNNeighbors { get; init; }

  /// <summary>UMAP <c>min_dist</c> for the 2D atlas-display reduction (typically 0.1).</summary>
  public double Umap2DMinDist { get; init; }
}
