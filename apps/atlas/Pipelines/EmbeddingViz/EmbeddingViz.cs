using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Helpers.Nodes;
using MagicAtlas.Pipelines.EmbeddingViz.Nodes;

namespace MagicAtlas.Pipelines.EmbeddingViz;

/// <summary>
/// Pipeline for creating enhanced visualizations of UMAP embeddings with card metadata.
/// </summary>
/// <remarks>
/// <para>
/// Enriches UMAP embeddings with card properties (name, color identity, mana value)
/// and generates an enhanced scatter plot where point size represents CMC and color
/// represents card color identity using MTG canonical colors.
/// </para>
/// <para>
/// <strong>Pipeline Flow:</strong>
/// </para>
/// <list type="number">
/// <item>Join UMAP embeddings with card core data</item>
/// <item>Generate enhanced scatter plot with CMC sizing and color identity coloring</item>
/// <item>Export visualization to PNG</item>
/// </list>
/// </remarks>
public static class EmbeddingViz
{
  /// <summary>
  /// Configuration parameters for the embedding visualization pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for the CMC-based scatter plot visualization.
    /// </summary>
    public GenerateEnhancedScatterPlotNode.Params ScatterPlotByCmcOptions { get; init; } = new();

    /// <summary>
    /// Configuration options for the price-based scatter plot visualization.
    /// </summary>
    public GenerateEnhancedScatterPlotNode.Params ScatterPlotByPriceOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the embedding visualization pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>
  /// A configured pipeline that enhances UMAP embeddings and creates rich visualizations.
  /// </returns>
  public static Pipeline Create(Catalog catalog, Params? parameters = null)
  {
    var opts = parameters ?? new Params();

    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "EnhanceEmbeddingData",
        description: """
          Joins UMAP embeddings with card core data to add card name, color identity,
          mana value (CMC), and price for enhanced visualizations.
        """,
        transform: EnhanceEmbeddingDataNode.Create(),
        input: (catalog.OracleUmapEmbeddings, catalog.FilteredCardCoreData),
        output: (catalog.EnhancedUmapEmbeddings, catalog.EnhancedUmapFlattenedEmbeddings)
      );

      pipeline.AddNode(
        label: "GenerateEnhancedScatterPlotByCmc",
        description: """
          Generates a 2D scatter plot of UMAP embeddings with:
          - Position from UMAP coordinates
          - Size proportional to card CMC
          - Color based on card color identity (MTG colors)
        """,
        transform: GenerateEnhancedScatterPlotNode.Create(opts.ScatterPlotByCmcOptions),
        input: catalog.EnhancedUmapEmbeddings,
        output: catalog.EnhancedUmapScatterPlotChart
      );

      pipeline.AddNode(
        label: "ExportEnhancedScatterPlotByCmc",
        description: """
          Exports the enhanced UMAP scatter plot (by CMC) to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.EnhancedUmapScatterPlotChart,
        output: catalog.EnhancedUmapScatterPlotPng
      );

      pipeline.AddNode(
        label: "GenerateEnhancedScatterPlotByPrice",
        description: """
          Generates a 2D scatter plot of UMAP embeddings with:
          - Position from UMAP coordinates
          - Size proportional to card price
          - Color based on card color identity (MTG colors)
        """,
        transform: GenerateEnhancedScatterPlotNode.Create(opts.ScatterPlotByPriceOptions),
        input: catalog.EnhancedUmapEmbeddings,
        output: catalog.EnhancedUmapScatterPlotByPriceChart
      );

      pipeline.AddNode(
        label: "ExportEnhancedScatterPlotByPrice",
        description: """
          Exports the enhanced UMAP scatter plot (by price) to PNG format for viewing and sharing.
        """,
        transform: PlotlyImageExportNode.Create(),
        input: catalog.EnhancedUmapScatterPlotByPriceChart,
        output: catalog.EnhancedUmapScatterPlotByPricePng
      );
    });
  }
}
