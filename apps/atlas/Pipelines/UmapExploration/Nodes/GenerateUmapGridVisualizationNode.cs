using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data.Enums.Card;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace MagicAtlas.Pipelines.UmapExploration.Nodes;

/// <summary>
/// Generates a grid visualization of UMAP hyperparameter scan results.
/// </summary>
/// <remarks>
/// <para>
/// Creates a multi-panel figure where each subplot shows UMAP embeddings generated
/// with different hyperparameter combinations. All coordinates are normalized to [0,1]
/// for consistent visual comparison across different UMAP runs.
/// </para>
/// <para>
/// <strong>Grid Layout:</strong> Rows represent different NumberOfNeighbors values,
/// columns represent different MinDist values. Each subplot is titled with its
/// hyperparameter combination.
/// </para>
/// <para>
/// <strong>Normalization Strategy:</strong> Min-max normalization applied independently
/// to each UMAP result to map all coordinates to [0,1] range. This enables visual
/// comparison of cluster patterns regardless of absolute coordinate scales.
/// </para>
/// <para>
/// <strong>Color Coding:</strong> Points colored by OracleTextType consistently across
/// all subplots for easy pattern recognition.
/// </para>
/// </remarks>
public static class GenerateUmapGridVisualizationNode
{
  /// <summary>
  /// Configuration options for grid visualization.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Width of the output figure in pixels.
    /// </summary>
    public int Width { get; init; } = 2560;

    /// <summary>
    /// Height of the output figure in pixels.
    /// </summary>
    public int Height { get; init; } = 2560;

    /// <summary>
    /// Marker size for scatter points.
    /// </summary>
    public int MarkerSize { get; init; } = 3;

    /// <summary>
    /// Marker opacity (0.0-1.0).
    /// </summary>
    public float MarkerOpacity { get; init; } = 0.7f;
  }

  /// <summary>
  /// Creates a grid visualization generation function.
  /// </summary>
  /// <param name="options">Configuration for the visualization.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// Transform function that generates a GenericChart grid from scan results.
  /// </returns>
  public static Func<IEnumerable<UmapHyperparameterScanResult>, Task<GenericChart>> Create(
    Params? options = null,
    ILogger? logger = null
  )
  {
    var opts = options ?? new Params();

    return async (scanResults) =>
    {
      var results = scanResults.ToList();

      if (results.Count == 0)
      {
        throw new InvalidOperationException("Cannot generate grid from empty scan results");
      }

      logger?.LogInformation(
        "Generating UMAP grid visualization from {Count} scan results",
        results.Count
      );

      // Group by hyperparameter combinations
      var groupedResults = results
        .GroupBy(r => new { r.NumberOfNeighbors, r.MinDist })
        .OrderBy(g => g.Key.NumberOfNeighbors)
        .ThenBy(g => g.Key.MinDist)
        .ToList();

      logger?.LogInformation(
        "Found {CombinationCount} hyperparameter combinations",
        groupedResults.Count
      );

      // Determine grid dimensions
      var uniqueNeighbors = groupedResults
        .Select(g => g.Key.NumberOfNeighbors)
        .Distinct()
        .OrderBy(n => n)
        .ToArray();
      var uniqueMinDist = groupedResults
        .Select(g => g.Key.MinDist)
        .Distinct()
        .OrderBy(m => m)
        .ToArray();
      var nRows = uniqueNeighbors.Length;
      var nCols = uniqueMinDist.Length;

      logger?.LogInformation(
        "Grid layout: {Rows} rows (neighbors) × {Cols} cols (minDist)",
        nRows,
        nCols
      );

      // Create subplots
      var subplots = new List<GenericChart>();
      var subplotTitles = new List<string>();

      foreach (var group in groupedResults)
      {
        var neighbors = group.Key.NumberOfNeighbors;
        var minDist = group.Key.MinDist;
        var embeddings = group.ToList();

        logger?.LogInformation(
          "Creating subplot for N={Neighbors}, MinDist={MinDist} ({Count} points)",
          neighbors,
          minDist,
          embeddings.Count
        );

        // Normalize coordinates to [0,1] range
        var normalized = NormalizeCoordinates(embeddings);

        // Create scatter plot for this combination
        var subplot = CreateScatterPlot(normalized, opts);
        subplots.Add(subplot);

        // Create subplot title
        subplotTitles.Add($"N={neighbors}, MinDist={minDist:F2}");
      }

      // Combine into grid layout
      var gridChart = CSharpChart
        .Grid(subplots.ToArray(), nRows, nCols, SubPlotTitles: subplotTitles.ToArray())
        .WithTitle("UMAP Hyperparameter Exploration")
        .WithSize(opts.Width, opts.Height);

      logger?.LogInformation("Grid visualization generated successfully");

      return await Task.FromResult(gridChart);
    };
  }

  /// <summary>
  /// Normalizes UMAP coordinates to [0,1] range using min-max normalization.
  /// </summary>
  private static List<(UmapHyperparameterScanResult result, float x, float y)> NormalizeCoordinates(
    List<UmapHyperparameterScanResult> embeddings
  )
  {
    // Extract X and Y coordinates
    var xValues = embeddings.Select(e => e.Components[0]).ToArray();
    var yValues = embeddings.Select(e => e.Components[1]).ToArray();

    var xMin = xValues.Min();
    var xMax = xValues.Max();
    var yMin = yValues.Min();
    var yMax = yValues.Max();

    // Normalize to [0, 1]
    var xRange = xMax - xMin;
    var yRange = yMax - yMin;

    return embeddings
      .Select(e =>
      {
        var xNorm = xRange > 0 ? (e.Components[0] - xMin) / xRange : 0.5f;
        var yNorm = yRange > 0 ? (e.Components[1] - yMin) / yRange : 0.5f;
        return (result: e, x: xNorm, y: yNorm);
      })
      .ToList();
  }

  /// <summary>
  /// Creates a scatter plot for a single hyperparameter combination.
  /// </summary>
  private static GenericChart CreateScatterPlot(
    List<(UmapHyperparameterScanResult result, float x, float y)> normalizedData,
    Params opts
  )
  {
    // Group by text type for color coding
    var groupedByType = normalizedData.GroupBy(d => d.result.TextType).OrderBy(g => g.Key).ToList();

    var traces = new List<GenericChart>();

    foreach (var group in groupedByType)
    {
      var textType = group.Key;
      var points = group.ToList();

      var xValues = points.Select(p => p.x).ToArray();
      var yValues = points.Select(p => p.y).ToArray();

      // Create hover text
      var hoverText = points
        .Select(p =>
          $"Card: {p.result.CardId}<br>Type: {textType}<br>Text: {TruncateText(p.result.Text, 80)}"
        )
        .ToArray();

      var trace = CSharpChart
        .Point<float, float, string>(x: xValues, y: yValues, MultiText: hoverText)
        .WithTraceInfo(Name: GetTextTypeName(textType), ShowLegend: true)
        .WithMarkerStyle(
          Color: Color.fromKeyword(GetColorForTextType(textType)),
          Size: opts.MarkerSize,
          Opacity: opts.MarkerOpacity
        );

      traces.Add(trace);
    }

    return CSharpChart
      .Combine(traces)
      .WithXAxisStyle(Title.init("UMAP Component 1 (normalized)"))
      .WithYAxisStyle(Title.init("UMAP Component 2 (normalized)"));
  }

  /// <summary>
  /// Gets display name for text type.
  /// </summary>
  private static string GetTextTypeName(OracleTextType textType)
  {
    return textType switch
    {
      OracleTextType.KeywordAbility => "Keyword",
      OracleTextType.NamedTriggeredAbility => "Named Trigger",
      OracleTextType.ActivatedAbility => "Activated",
      OracleTextType.TriggeredAbility => "Triggered",
      OracleTextType.PassiveAbility => "Passive",
      OracleTextType.Full => "Full",
      _ => textType.ToString(),
    };
  }

  /// <summary>
  /// Assigns color to text type.
  /// </summary>
  private static ColorKeyword GetColorForTextType(OracleTextType textType)
  {
    return textType switch
    {
      OracleTextType.KeywordAbility => ColorKeyword.Blue,
      OracleTextType.NamedTriggeredAbility => ColorKeyword.Green,
      OracleTextType.ActivatedAbility => ColorKeyword.Red,
      OracleTextType.TriggeredAbility => ColorKeyword.Orange,
      OracleTextType.PassiveAbility => ColorKeyword.Purple,
      OracleTextType.Full => ColorKeyword.Gray,
      _ => ColorKeyword.Black,
    };
  }

  /// <summary>
  /// Truncates text for hover display.
  /// </summary>
  private static string TruncateText(string text, int maxLength)
  {
    if (text.Length <= maxLength)
    {
      return text;
    }
    return text.Substring(0, maxLength) + "...";
  }
}
