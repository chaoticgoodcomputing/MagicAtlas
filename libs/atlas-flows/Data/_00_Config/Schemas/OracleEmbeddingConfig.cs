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

  /// <summary>UMAP <c>n_neighbors</c> for the 5D→2D visualization reduction. Post-restructure
  /// the 2D step is UNSUPERVISED — it just preserves the topology of the already-structured 5D
  /// embedding. Smaller values (15–30) emphasize local detail; larger pulls in global shape.</summary>
  public int Umap2DNNeighbors { get; init; }

  /// <summary>UMAP <c>min_dist</c> for the 5D→2D visualization reduction. Visual spacing knob;
  /// 0.1 is the umap-learn default and tends to look right.</summary>
  public double Umap2DMinDist { get; init; }

  /// <summary>
  /// Pre-UMAP Gaussian jitter sigma applied at the HD→5D step (scaled by the embedding norm).
  /// Spreads identical-text lines so the 5D and downstream 2D projections don't collapse them
  /// to identical points. Default <c>0.0001</c>; set to <c>0</c> to disable.
  /// </summary>
  public double UmapJitterSigma { get; init; }
}
