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
  // ── UMAP, 5D target for HDBSCAN ──
  public int Umap5DNNeighbors { get; init; }

  /// <summary>BERTopic recommendation for clustering-target UMAPs is 0.0 — tighter local
  /// structure helps HDBSCAN separate dense regions.</summary>
  public double Umap5DMinDist { get; init; }

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
