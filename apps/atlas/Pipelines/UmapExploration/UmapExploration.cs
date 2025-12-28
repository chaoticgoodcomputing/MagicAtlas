using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Helpers.Nodes;
using MagicAtlas.Pipelines.UmapExploration.Nodes;

namespace MagicAtlas.Pipelines.UmapExploration;

/// <summary>
/// Pipeline for exploring UMAP hyperparameter sensitivity through grid search visualization.
/// </summary>
/// <remarks>
/// <para>
/// Performs systematic exploration of UMAP hyperparameters by generating embeddings
/// across multiple combinations of NumberOfNeighbors and MinDist values, then
/// visualizing all results in a normalized grid layout for comparison.
/// </para>
/// <para>
/// <strong>Pipeline Stages:</strong>
/// </para>
/// <list type="number">
/// <item><strong>Filter & Sample:</strong> Remove unwanted text types and sample a percentage for faster exploration</item>
/// <item><strong>Hyperparameter Scan:</strong> Generate UMAP embeddings for all hyperparameter combinations</item>
/// <item><strong>Grid Visualization:</strong> Create multi-panel figure with normalized coordinates</item>
/// <item><strong>PNG Export:</strong> Save visualization as high-resolution image</item>
/// </list>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Understanding how NumberOfNeighbors affects local vs. global structure</item>
/// <item>Evaluating MinDist impact on cluster spacing and separation</item>
/// <item>Selecting optimal hyperparameters for final UMAP visualizations</item>
/// <item>Validating UMAP stability across parameter ranges</item>
/// </list>
/// <para>
/// <strong>Performance Tip:</strong> Use aggressive sampling (e.g., 10-20%) for initial
/// exploration, then refine with larger samples once promising parameters are identified.
/// </para>
/// </remarks>
public static class UmapExploration
{
  /// <summary>
  /// Configuration parameters for the UMAP exploration pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration for filtering and sampling embeddings.
    /// </summary>
    public FilterAndSampleEmbeddingsNode.Params FilterSampleOptions { get; init; } = new();

    /// <summary>
    /// Configuration for UMAP hyperparameter scanning.
    /// </summary>
    public UmapHyperparameterScanNode.Params ScanOptions { get; init; } =
      new() { NumberOfNeighborsValues = [15], MinDistValues = [0.1f] };

    /// <summary>
    /// Configuration for grid visualization.
    /// </summary>
    public GenerateUmapGridVisualizationNode.Params VisualizationOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the UMAP exploration pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that performs UMAP hyperparameter exploration and visualization.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params? parameters = null)
  {
    var opts = parameters ?? new Params();

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "FilterAndSample",
        description: """
          Filters oracle text embeddings by type and samples a percentage for exploration.
          First removes excluded text types (e.g., Full text), then randomly samples
          from the remaining embeddings using the specified percentage and seed.
        """,
        transform: FilterAndSampleEmbeddingsNode.Create(opts.FilterSampleOptions),
        input: catalog.OracleTextEmbeddings,
        output: catalog.FilteredSampledOracleTextEmbeddings
      );

      pipeline.AddNode(
        label: "UmapHyperparameterScan",
        description: """
          Performs UMAP dimensionality reduction with multiple hyperparameter combinations.
          Generates a cartesian product of NumberOfNeighbors and MinDist values,
          running UMAP for each combination to enable systematic parameter exploration.
        """,
        transform: UmapHyperparameterScanNode.Create(opts.ScanOptions),
        input: catalog.FilteredSampledOracleTextEmbeddings,
        output: catalog.UmapHyperparameterScanResults
      );

      pipeline.AddNode(
        label: "GenerateGridVisualization",
        description: """
          Creates a grid visualization of all UMAP hyperparameter combinations.
          Each subplot shows embeddings with different parameters, with coordinates
          normalized to [0,1] for consistent comparison. Points colored by text type.
        """,
        transform: GenerateUmapGridVisualizationNode.Create(opts.VisualizationOptions),
        input: catalog.UmapHyperparameterScanResults,
        output: catalog.UmapGridVisualizationChart
      );

      pipeline.AddNode(
        label: "ExportGridVisualization",
        description: """
          Exports the UMAP grid visualization to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.UmapGridVisualizationChart,
        output: catalog.UmapGridVisualizationPng
      );
    });
  }
}
