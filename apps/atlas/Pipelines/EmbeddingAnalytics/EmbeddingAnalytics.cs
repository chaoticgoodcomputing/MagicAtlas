using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Helpers.Nodes;
using MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics;

/// <summary>
/// Diagnostic pipeline for analyzing card embeddings through nearest neighbor search.
/// </summary>
public static class EmbeddingAnalytics
{
  /// <summary>
  /// Configuration parameters for the diagnostics pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for the nearest neighbor sampling node.
    /// </summary>
    public SampleOracleNearestNeighborsNode.Params NearestNeighborOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the embedding sampling node.
    /// </summary>
    public SampleOracleTextEmbeddingsNode.Params EmbeddingSamplingParams { get; init; } = new();

    /// <summary>
    /// Configuration options for the embedding distribution chart.
    /// </summary>
    public GenerateEmbeddingDistributionChartNode.Params DistributionChartParams { get; init; } =
      new();

    /// <summary>
    /// Configuration options for K-means clustering.
    /// </summary>
    public KMeansClusteringNode.Params ClusteringParams { get; init; } = new();

    /// <summary>
    /// Configuration options for enriching cluster metadata (top-N closest per cluster).
    /// </summary>
    public EnrichClusterMetadataNode.Params EnrichmentParams { get; init; } = new();
  }

  /// <summary>
  /// Creates the diagnostics pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that samples cards and finds their nearest neighbors in embedding space.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "SampleOracleNearestNeighbors",
        description: """
          Samples oracle cards and finds their nearest neighbors in embedding space based
          on ability similarity.
        """,
        transform: SampleOracleNearestNeighborsNode.Create(parameters.NearestNeighborOptions),
        input: (
          catalog.FilteredCardCoreData,
          catalog.FilteredCardMetadata,
          catalog.OracleTextEmbeddings
        ),
        output: catalog.NearestNeighborAnalysis
      );

      pipeline.AddNode(
        label: "SampleOracleTextEmbeddings",
        description: """
          Randomly samples N oracle text embeddings for distribution analysis and visualization.
        """,
        transform: SampleOracleTextEmbeddingsNode.Create(parameters.EmbeddingSamplingParams),
        input: catalog.OracleTextEmbeddings,
        output: catalog.SampledOracleTextEmbeddings
      );

      pipeline.AddNode(
        label: "GenerateEmbeddingDistributionChart",
        description: """
          Creates overlapping line charts showing the distribution of values across all 384
          embedding dimensions, with normalized Y-axis showing percentage of observations.
        """,
        transform: GenerateEmbeddingDistributionChartNode.Create(
          parameters.DistributionChartParams
        ),
        input: catalog.OracleTextEmbeddings,
        output: catalog.EmbeddingDistributionChart
      );

      pipeline.AddNode(
        label: "ExportEmbeddingDistributionPng",
        description: """
          Exports the embedding distribution chart to PNG format for static reports and analysis.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.EmbeddingDistributionChart,
        output: catalog.EmbeddingDistributionPlotPng
      );

      pipeline.AddNode(
        label: "ClusterOracleTextEmbeddings",
        description: """
          Performs K-means clustering on oracle text embeddings to identify groups of
          mechanically similar cards. Outputs cluster assignments and per-cluster statistics.
        """,
        transform: KMeansClusteringNode.Create(parameters.ClusteringParams),
        input: catalog.OracleTextEmbeddings,
        output: (catalog.OracleTextClusterAssignments, catalog.KMeansClusterMetadata)
      );

      pipeline.AddNode(
        label: "EnrichClusterMetadata",
        description: """
          Enriches cluster metadata with card information to provide interpretable context
          about what each cluster represents through its most representative card.
        """,
        transform: EnrichClusterMetadataNode.Create(parameters.EnrichmentParams),
        input: (
          catalog.KMeansClusterMetadata,
          catalog.OracleTextClusterAssignments,
          catalog.FilteredCardCoreData,
          catalog.FilteredCardMetadata
        ),
        output: catalog.EnrichedClusterMetadata
      );
    });
  }
}
