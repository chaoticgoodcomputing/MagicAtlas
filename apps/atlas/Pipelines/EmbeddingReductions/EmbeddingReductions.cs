using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Helpers.Nodes;
using MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

namespace MagicAtlas.Pipelines.EmbeddingReductions;

/// <summary>
/// Pipeline for performing dimensionality reduction on oracle text embeddings using PCA and UMAP.
/// </summary>
/// <remarks>
/// <para>
/// Reduces 384-dimensional sentence embeddings to lower-dimensional representations
/// using two complementary techniques:
/// </para>
/// <list type="bullet">
/// <item><strong>PCA:</strong> Linear dimensionality reduction preserving maximum variance</item>
/// <item><strong>UMAP:</strong> Manifold learning preserving local and global structure</item>
/// </list>
/// </remarks>
public static class EmbeddingReductions
{
  /// <summary>
  /// Configuration parameters for the embedding reductions pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for the PCA reduction node.
    /// </summary>
    public PcaReductionNode.Params PcaOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the UMAP reduction node.
    /// </summary>
    public UmapReductionNode.Params UmapOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the PCA scatter plot visualization.
    /// </summary>
    public GeneratePcaScatterPlotNode.Params PcaScatterPlotOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the UMAP scatter plot visualization.
    /// </summary>
    public GenerateUmapScatterPlotNode.Params UmapScatterPlotOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the embedding reductions pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that performs PCA and UMAP dimensionality reduction on embeddings.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params? parameters = null)
  {
    var opts = parameters ?? new Params();

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "PcaReduction",
        description: """
          Performs PCA dimensionality reduction on oracle text embeddings.
          Reduces 384-dimensional vectors to a lower-dimensional representation.
        """,
        transform: PcaReductionNode.Create(opts.PcaOptions),
        input: catalog.OracleTextEmbeddings,
        output: catalog.OraclePcaEmbeddings
      );

      pipeline.AddNode(
        label: "GeneratePcaScatterPlot",
        description: """
          Generates a 2D scatter plot of the first two PCA components.
          Filters out Full text entries and colors points by oracle text type.
        """,
        transform: GeneratePcaScatterPlotNode.Create(opts.PcaScatterPlotOptions),
        input: catalog.OraclePcaEmbeddings,
        output: catalog.PcaScatterPlotChart
      );

      pipeline.AddNode(
        label: "ExportPcaScatterPlot",
        description: """
          Exports the PCA scatter plot to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.PcaScatterPlotChart,
        output: catalog.PcaScatterPlotPng
      );

      pipeline.AddNode(
        label: "UmapReduction",
        description: """
          Performs UMAP dimensionality reduction on oracle text embeddings.
          Uses manifold learning to preserve both local and global structure,
          typically producing superior visualizations compared to linear PCA.
        """,
        transform: UmapReductionNode.Create(opts.UmapOptions),
        input: catalog.OracleTextEmbeddings,
        output: catalog.OracleUmapEmbeddings
      );

      pipeline.AddNode(
        label: "GenerateUmapScatterPlot",
        description: """
          Generates a 2D scatter plot of the first two UMAP components.
          Filters out Full text entries and colors points by oracle text type.
        """,
        transform: GenerateUmapScatterPlotNode.Create(opts.UmapScatterPlotOptions),
        input: catalog.OracleUmapEmbeddings,
        output: catalog.UmapScatterPlotChart
      );

      pipeline.AddNode(
        label: "ExportUmapScatterPlot",
        description: """
          Exports the UMAP scatter plot to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.UmapScatterPlotChart,
        output: catalog.UmapScatterPlotPng
      );
    });
  }
}
