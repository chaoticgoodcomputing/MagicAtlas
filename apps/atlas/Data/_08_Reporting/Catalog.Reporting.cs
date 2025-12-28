using Flowthru.Data;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using Plotly.NET;

namespace MagicAtlas.Data;

/// <summary>
/// Reporting data catalog entries (Layer 8).
/// Contains ad hoc descriptive analyses and diagnostic outputs.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Nearest neighbor analysis results for sampled oracle cards.
  /// </summary>
  public ICatalogEntry<IEnumerable<NearestNeighborAnalysis>> NearestNeighborAnalysis =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<NearestNeighborAnalysis>(
          label: "NearestNeighborAnalysis",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/nearest_neighbor_analysis.json"
        )
    );

  /// <summary>
  /// Randomly sampled oracle text embeddings for distribution analysis.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextEmbedding>> SampledOracleTextEmbeddings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<OracleTextEmbedding>(
          label: "SampledOracleTextEmbeddings",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/sampled_oracle_embeddings.json"
        )
    );

  /// <summary>
  /// In-memory chart showing distribution of embedding dimensions.
  /// </summary>
  public ICatalogEntry<GenericChart> EmbeddingDistributionChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "EmbeddingDistributionChart")
    );

  /// <summary>
  /// PNG export of embedding distribution chart.
  /// </summary>
  public ICatalogEntry<byte[]> EmbeddingDistributionPlotPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "EmbeddingDistributionPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/embedding_distribution_plot.png"
        )
    );

  /// <summary>
  /// K-means cluster assignments for oracle text embeddings.
  /// Maps each embedding to its assigned cluster ID and distance.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextClusterAssignment>> OracleTextClusterAssignments =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<OracleTextClusterAssignment>(
          label: "OracleTextClusterAssignments",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/oracle_text_cluster_assignments.csv"
        )
    );

  /// <summary>
  /// Statistical metadata for K-means clustering results.
  /// Contains per-cluster statistics including mean distance and standard deviation.
  /// </summary>
  public ICatalogEntry<KMeansClusterMetadata> KMeansClusterMetadata =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<KMeansClusterMetadata>(
          label: "KMeansClusterMetadata",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/kmeans_cluster_metadata.json"
        )
    );

  /// <summary>
  /// K-means cluster metadata enriched with per-cluster lists of the N closest observations
  /// joined to card data for interpretability.
  /// </summary>
  public ICatalogEntry<EnrichedClusterMetadata> EnrichedClusterMetadata =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<EnrichedClusterMetadata>(
          label: "EnrichedClusterMetadata",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/enriched_cluster_metadata.json"
        )
    );

  /// <summary>
  /// In-memory chart showing PCA scatter plot of oracle text embeddings.
  /// First two principal components colored by text type.
  /// </summary>
  public ICatalogEntry<GenericChart> PcaScatterPlotChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "PcaScatterPlotChart")
    );

  /// <summary>
  /// PNG export of PCA scatter plot chart.
  /// </summary>
  public ICatalogEntry<byte[]> PcaScatterPlotPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "PcaScatterPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/PCA/pca_scatter_plot.png"
        )
    );

  /// <summary>
  /// In-memory chart showing UMAP scatter plot of oracle text embeddings.
  /// First two UMAP components colored by text type, preserving manifold structure.
  /// </summary>
  public ICatalogEntry<GenericChart> UmapScatterPlotChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "UmapScatterPlotChart")
    );

  /// <summary>
  /// PNG export of UMAP scatter plot chart.
  /// </summary>
  public ICatalogEntry<byte[]> UmapScatterPlotPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "UmapScatterPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/umap_scatter_plot.png"
        )
    );

  /// <summary>
  /// In-memory chart showing enhanced UMAP scatter plot with card metadata.
  /// Points sized by CMC and colored by card color identity.
  /// </summary>
  public ICatalogEntry<GenericChart> EnhancedUmapScatterPlotChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "EnhancedUmapScatterPlotChart")
    );

  /// <summary>
  /// PNG export of enhanced UMAP scatter plot chart (sized by CMC).
  /// </summary>
  public ICatalogEntry<byte[]> EnhancedUmapScatterPlotPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "EnhancedUmapScatterPlotPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/enhanced_umap_scatter_plot.png"
        )
    );

  /// <summary>
  /// In-memory chart showing enhanced UMAP scatter plot with card metadata.
  /// Points sized by price and colored by card color identity.
  /// </summary>
  public ICatalogEntry<GenericChart> EnhancedUmapScatterPlotByPriceChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "EnhancedUmapScatterPlotByPriceChart")
    );

  /// <summary>
  /// PNG export of enhanced UMAP scatter plot chart (sized by price).
  /// </summary>
  public ICatalogEntry<byte[]> EnhancedUmapScatterPlotByPricePng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "EnhancedUmapScatterPlotByPricePng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/enhanced_umap_scatter_plot_by_price.png"
        )
    );

  /// <summary>
  /// UMAP embeddings enhanced with card metadata for rich visualizations.
  /// </summary>
  public ICatalogEntry<
    IEnumerable<EnhancedUmapFlattenedEmbedding>
  > EnhancedUmapFlattenedEmbeddings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Csv<EnhancedUmapFlattenedEmbedding>(
          label: "EnhancedUmapFlattenedEmbeddings",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/enhanced_umap_flattened_embeddings.csv"
        )
    );

  /// <summary>
  /// Oracle text frequency analysis results.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextDuplicateCount>> OracleTextFrequencyAnalysis =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<OracleTextDuplicateCount>(
          label: "OracleTextFrequencyAnalysis",
          filePath: $"{_basePath}/_08_Reporting/Datasets/Exploratory/oracle_text_frequency_analysis.json"
        )
    );

  /// <summary>
  /// Filtered and randomly sampled oracle text embeddings for UMAP exploration.
  /// </summary>
  public ICatalogEntry<IEnumerable<OracleTextEmbedding>> FilteredSampledOracleTextEmbeddings =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<OracleTextEmbedding>(
          label: "FilteredSampledOracleTextEmbeddings",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/filtered_sampled_oracle_embeddings.json"
        )
    );

  /// <summary>
  /// UMAP hyperparameter grid search results.
  /// Contains multiple UMAP embeddings generated with different hyperparameter combinations
  /// for sensitivity analysis and optimal parameter selection.
  /// </summary>
  public ICatalogEntry<IEnumerable<UmapHyperparameterScanResult>> UmapHyperparameterScanResults =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Parquet<UmapHyperparameterScanResult>(
          label: "UmapHyperparameterScanResults",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/umap_hyperparameter_scan_results.parquet"
        )
    );

  /// <summary>
  /// In-memory chart showing UMAP hyperparameter grid exploration.
  /// Multiple subplots arranged in a grid, each showing UMAP embeddings with different
  /// hyperparameter combinations (NumberOfNeighbors, MinDist).
  /// </summary>
  public ICatalogEntry<GenericChart> UmapGridVisualizationChart =>
    GetOrCreateEntry(
      () => CatalogEntries.Single.Memory<GenericChart>(label: "UmapGridVisualizationChart")
    );

  /// <summary>
  /// PNG export of UMAP hyperparameter grid visualization.
  /// </summary>
  public ICatalogEntry<byte[]> UmapGridVisualizationPng =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Binary(
          label: "UmapGridVisualizationPng",
          filePath: $"{_basePath}/_08_Reporting/Datasets/UMAP/umap_grid_visualization.png"
        )
    );

  /// <summary>
  /// CardOutputDto with AST analysis results and diagnostics.
  /// Persisted as JSON for review and debugging.
  /// </summary>
  public ICatalogEntry<IEnumerable<MagicAstCardAnalysis>> MagicAstAnalysisResults =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<MagicAstCardAnalysis>(
          label: "MagicAstAnalysisResults",
          filePath: $"{_basePath}/_08_Reporting/Datasets/MagicAST/analysis_results.json"
        )
    );

  /// <summary>
  /// Comprehensive parsing report for errors across all cards.
  /// Includes card-level and ability-level statistics with categorized error patterns.
  /// </summary>
  public ICatalogEntry<MagicAstParsingReport> MagicAstErrorReport =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<MagicAstParsingReport>(
          label: "MagicAstErrorReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/MagicAST/error_report.json"
        )
    );

  /// <summary>
  /// Comprehensive parsing report for warnings across all cards.
  /// Includes card-level and ability-level statistics with categorized warning patterns.
  /// </summary>
  public ICatalogEntry<MagicAstParsingReport> MagicAstWarningReport =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<MagicAstParsingReport>(
          label: "MagicAstWarningReport",
          filePath: $"{_basePath}/_08_Reporting/Datasets/MagicAST/warning_report.json"
        )
    );
}
