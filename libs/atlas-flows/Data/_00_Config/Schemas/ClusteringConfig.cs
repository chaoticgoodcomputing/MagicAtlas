using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the Clustering flow — UMAP-to-5D, HDBSCAN, and c-TF-IDF labeling knobs.
/// Materialized at startup from <c>Flowthru:Flows:Clustering</c> in <c>appsettings.json</c>.
/// All fields are flat scalars (Flowthru's Python marshaller can't encode nested POCOs as step
/// inputs); the <c>Umap5D*</c> / <c>Hdbscan*</c> / <c>CTfIdf*</c> prefixes group fields visually
/// without nesting them in sub-records.
/// </summary>
[FlowthruSchema]
public partial record ClusteringConfig
{
  // ── UMAP, 5D target — the SUPERVISED structuring layer ──
  // After the HD→5D→2D restructure, the 5D step carries supervision pressure (canonical-label
  // categorical UMAP). The downstream 2D step is unsupervised topology preservation of this
  // already-structured 5D space.
  public int Umap5DNNeighbors { get; init; }

  /// <summary>BERTopic recommendation for clustering-target UMAPs is 0.0 — tighter local
  /// structure helps HDBSCAN separate dense regions, and also gives the downstream unsupervised
  /// 2D step a denser low-D manifold to project.</summary>
  public double Umap5DMinDist { get; init; }

  /// <summary>
  /// Master toggle for supervised UMAP at HD→5D. When <c>true</c>, the step passes
  /// <c>LinePrimaryCanonicals</c> as the y label and applies <see cref="Umap5DSupervisionWeight"/>.
  /// When <c>false</c>, the step runs purely unsupervised and the canonical input is ignored —
  /// the 5D output reflects only the embedding's intrinsic structure. Useful for comparing
  /// supervised vs unsupervised 5D in the eval scorecard.
  /// </summary>
  public bool Umap5DSupervised { get; init; } = true;

  /// <summary>
  /// Weight of the supervised canonical-label signal vs. the unsupervised X-similarity signal
  /// in the HD→5D UMAP. Range <c>0</c>..<c>1</c>: <c>0</c> = pure unsupervised, <c>1</c> = pure
  /// categorical. Only applied when <see cref="Umap5DSupervised"/> is <c>true</c>. With ~280
  /// canonicals competing for 5 dimensions, supervision succeeds where it can't at 2D — 5 dims
  /// have room for the leaf structure that 2D cannot fit. Default <c>0.7</c>.
  /// </summary>
  public double Umap5DSupervisionWeight { get; init; } = 0.7;

  // ── HDBSCAN ──
  public int HdbscanMinClusterSize { get; init; }
  public int HdbscanMinSamples { get; init; }

  // ── c-TF-IDF labeling ──

  /// <summary>Total keywords stored per cluster in <c>keywords</c>.</summary>
  public int CTfIdfTopKeywords { get; init; }

  /// <summary>How many leading keywords compose the display <c>label</c>.</summary>
  public int CTfIdfLabelHead { get; init; }

  /// <summary>Inclusive lower bound for sklearn's <c>ngram_range</c>.</summary>
  public int CTfIdfNgramMin { get; init; }

  /// <summary>Inclusive upper bound for sklearn's <c>ngram_range</c>.</summary>
  public int CTfIdfNgramMax { get; init; }

  /// <summary>Minimum document frequency (across cluster pseudo-docs) for a token to be kept.</summary>
  public int CTfIdfMinDf { get; init; }

  /// <summary>
  /// Pre-UMAP Gaussian jitter sigma applied per row (scaled by the embedding norm). Same
  /// rationale as <c>OracleEmbeddingConfig.UmapJitterSigma</c>: spreads identical-text lines
  /// after dedup. Default <c>0.0001</c>; set to <c>0</c> to disable.
  /// </summary>
  public double UmapJitterSigma { get; init; }
}
