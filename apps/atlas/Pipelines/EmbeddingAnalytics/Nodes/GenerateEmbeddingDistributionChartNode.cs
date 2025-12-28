using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

/// <summary>
/// Generates a visualization showing the distribution of values across all embedding dimensions.
/// </summary>
/// <remarks>
/// <para>
/// Creates overlapping line charts where each line represents the distribution of one
/// embedding dimension. Values are binned into histograms and normalized to show
/// percentage of observations rather than raw counts.
/// </para>
/// <para>
/// <strong>Input:</strong> Oracle text embeddings (384 dimensions per embedding)
/// </para>
/// <para>
/// <strong>Output:</strong> GenericChart with 384 overlapping line traces
/// </para>
/// <para>
/// <strong>Visualization Purpose:</strong>
/// </para>
/// <list type="bullet">
/// <item>Identify if embedding dimensions follow expected distributions (e.g., Gaussian)</item>
/// <item>Detect anomalies or outliers in specific dimensions</item>
/// <item>Understand the overall statistical properties of the embedding space</item>
/// </list>
/// </remarks>
public static class GenerateEmbeddingDistributionChartNode
{
  /// <summary>
  /// Configuration options for the distribution chart.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of bins to use for histogram generation.
    /// Higher values provide more granular distribution detail.
    /// </summary>
    public int BinCount { get; init; } = 16;
  }

  /// <summary>
  /// Creates a chart generation function.
  /// </summary>
  /// <param name="options">Configuration for binning and visualization.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// A function that generates an embedding distribution chart from oracle text embeddings.
  /// </returns>
  public static Func<IEnumerable<OracleTextEmbedding>, Task<GenericChart>> Create(
    Params? options = null,
    ILogger? logger = null
  )
  {
    var config = options ?? new Params();

    return async (input) =>
    {
      var embeddings = input.ToList();

      if (embeddings.Count == 0)
      {
        throw new InvalidOperationException(
          "Cannot generate distribution chart from empty embedding dataset"
        );
      }

      var embeddingDimension = embeddings[0].EmbeddingDimension;

      logger?.LogInformation(
        "Generating embedding distribution chart for {EmbeddingCount} embeddings across {Dimensions} dimensions with {BinCount} bins",
        embeddings.Count,
        embeddingDimension,
        config.BinCount
      );

      // Create line traces for each dimension
      var traces = new List<GenericChart>();

      for (int dim = 0; dim < embeddingDimension; dim++)
      {
        var trace = CreateDimensionDistributionTrace(embeddings, dim, config.BinCount);
        traces.Add(trace);
      }

      // Combine all traces into a single chart with overlapping lines
      var chart = CSharpChart
        .Combine(traces)
        .WithXAxisStyle(Title.init("Embedding Value"))
        .WithYAxisStyle(Title.init("Percentage of Observations (%)"))
        .WithTitle(
          $"Embedding Dimension Distributions ({embeddingDimension} dimensions, {embeddings.Count} samples)"
        )
        .WithSize(1200, 800);

      logger?.LogInformation(
        "Successfully generated distribution chart with {TraceCount} overlapping line traces",
        traces.Count
      );

      return await Task.FromResult(chart);
    };
  }

  /// <summary>
  /// Creates a line trace showing the distribution of a single embedding dimension.
  /// </summary>
  private static GenericChart CreateDimensionDistributionTrace(
    List<OracleTextEmbedding> embeddings,
    int dimensionIndex,
    int binCount
  )
  {
    // Extract all values for this dimension
    var values = embeddings.Select(e => e.Embedding[dimensionIndex]).ToList();

    // Calculate min and max for binning
    var min = values.Min();
    var max = values.Max();

    // Create bin edges
    var binWidth = (max - min) / binCount;
    var binEdges = Enumerable.Range(0, binCount + 1).Select(i => min + i * binWidth).ToList();

    // Count observations in each bin
    var binCounts = new int[binCount];
    foreach (var value in values)
    {
      // Find which bin this value belongs to
      var binIndex = (int)Math.Floor((value - min) / binWidth);

      // Handle edge case where value equals max
      if (binIndex >= binCount)
      {
        binIndex = binCount - 1;
      }

      binCounts[binIndex]++;
    }

    // Calculate bin centers for x-axis
    var binCenters = Enumerable.Range(0, binCount).Select(i => min + (i + 0.5) * binWidth);

    // Normalize counts to percentages
    var totalCount = embeddings.Count;
    var percentages = binCounts.Select(count => (count / (double)totalCount) * 100.0);

    // Create line trace (no fill, just lines)
    return CSharpChart.Line<double, double, string>(
      binCenters,
      percentages,
      ShowLegend: false, // Don't show legend for 384 lines
      Opacity: 0.3 // Use transparency to handle overlap
    );
  }
}
