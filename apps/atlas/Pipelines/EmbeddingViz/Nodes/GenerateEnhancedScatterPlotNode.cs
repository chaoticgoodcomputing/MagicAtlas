using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data.Enums.Card;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace MagicAtlas.Pipelines.EmbeddingViz.Nodes;

/// <summary>
/// Generates an enhanced 2D scatter plot of UMAP embeddings with card metadata visualization.
/// </summary>
/// <remarks>
/// <para>
/// Creates a rich scatter plot where:
/// </para>
/// <list type="bullet">
/// <item><strong>Position:</strong> Determined by UMAP X and Y coordinates</item>
/// <item><strong>Size:</strong> Proportional to card mana value (CMC) or price</item>
/// <item><strong>Color:</strong> Based on card color identity using MTG canonical colors</item>
/// </list>
/// <para>
/// <strong>Color Mapping Strategy:</strong>
/// </para>
/// <list type="bullet">
/// <item>Colorless cards (no color identity): Gray</item>
/// <item>Monocolor cards: Respective MTG color</item>
/// <item>Multicolor cards: Randomly selected from constituent colors</item>
/// </list>
/// <para>
/// This visualization allows exploration of whether cards with similar oracle text
/// (as captured by UMAP) also share similar color identities, mana values, or prices.
/// </para>
/// </remarks>
public static class GenerateEnhancedScatterPlotNode
{
  /// <summary>
  /// Size metric for marker sizing in the scatter plot.
  /// </summary>
  public enum SizeMetric
  {
    /// <summary>
    /// Size markers proportionally to card mana value (CMC).
    /// </summary>
    Cmc,

    /// <summary>
    /// Size markers proportionally to card price (USD).
    /// </summary>
    Price,
  }

  /// <summary>
  /// Configuration options for the enhanced scatter plot.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Metric to use for sizing points in the scatter plot.
    /// </summary>
    /// <remarks>
    /// Default: Cmc
    /// </remarks>
    public SizeMetric SizingMetric { get; init; } = SizeMetric.Cmc;

    /// <summary>
    /// Multiplier for the sizing metric when calculating point size.
    /// </summary>
    /// <remarks>
    /// Default: 3.0 (CMC of 5 → marker size of 15, or $5 card → marker size of 15)
    /// Adjust based on desired visual prominence.
    /// </remarks>
    public float SizeMultiplier { get; init; } = 3.0f;

    /// <summary>
    /// Minimum marker size for zero-CMC cards.
    /// </summary>
    /// <remarks>
    /// Default: 2.0
    /// Ensures even zero-cost cards are visible.
    /// </remarks>
    public float MinimumMarkerSize { get; init; } = 2.0f;

    /// <summary>
    /// Opacity of markers in the scatter plot.
    /// </summary>
    /// <remarks>
    /// Default: 0.6
    /// Range: 0.0 (transparent) to 1.0 (opaque)
    /// </remarks>
    public float MarkerOpacity { get; init; } = 0.6f;

    /// <summary>
    /// Text types to exclude from visualization.
    /// Default: excludes Full text entries to focus on individual abilities.
    /// </summary>
    public HashSet<OracleTextType> ExcludedTextTypes { get; init; } = new() { OracleTextType.Full };

    /// <summary>
    /// Random seed for reproducible color selection in multicolor cards.
    /// </summary>
    public int? RandomSeed { get; init; } = 42;
  }

  /// <summary>
  /// Creates a scatter plot generation function.
  /// </summary>
  /// <param name="options">Configuration for visualization parameters.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// A function that generates an enhanced UMAP scatter plot from enriched embeddings.
  /// </returns>
  public static Func<IEnumerable<EnhancedUmapEmbedding>, Task<GenericChart>> Create(
    Params? options = null,
    ILogger? logger = null
  )
  {
    var config = options ?? new Params();
    var random = config.RandomSeed.HasValue ? new Random(config.RandomSeed.Value) : new Random();

    return async (input) =>
    {
      var embeddings = input.ToList();

      if (embeddings.Count == 0)
      {
        throw new InvalidOperationException(
          "Cannot generate scatter plot from empty embedding dataset"
        );
      }

      // Filter out excluded text types
      var filtered = embeddings.Where(e => !config.ExcludedTextTypes.Contains(e.TextType)).ToList();

      logger?.LogInformation(
        "Generating enhanced UMAP scatter plot: {TotalCount} embeddings → {FilteredCount} after filtering",
        embeddings.Count,
        filtered.Count
      );

      if (filtered.Count == 0)
      {
        throw new InvalidOperationException(
          "All embeddings were filtered out. Adjust ExcludedTextTypes configuration."
        );
      }

      // Verify we have at least 2 UMAP components
      var componentDim = filtered[0].ComponentDimension;
      if (componentDim < 2)
      {
        throw new InvalidOperationException(
          $"Cannot create 2D scatter plot with only {componentDim} UMAP component(s). Need at least 2."
        );
      }

      // Extract coordinates and metadata
      var xValues = filtered.Select(e => e.Components[0]).ToArray();
      var yValues = filtered.Select(e => e.Components[1]).ToArray();

      // Calculate marker sizes based on configured metric
      var sizes = filtered
        .Select(e =>
        {
          var metricValue = config.SizingMetric switch
          {
            SizeMetric.Cmc => (float)e.Cmc,
            SizeMetric.Price => (float)e.Price,
            _ => (float)e.Cmc,
          };
          var size = metricValue * config.SizeMultiplier;
          return Math.Max(size, config.MinimumMarkerSize);
        })
        .ToArray();

      // Assign a single color to each card based on color identity
      var colorKeywords = filtered.Select(e => GetColorForCard(e.ColorIdentity, random)).ToArray();

      // Create hover text with card info
      var hoverText = filtered
        .Select(e =>
        {
          var colorStr = FormatColorIdentity(e.ColorIdentity);
          return $"<b>{e.CardName}</b><br>"
            + $"Text: {TruncateText(e.Text, 100)}<br>"
            + $"CMC: {e.Cmc}<br>"
            + $"Price: ${e.Price:F2}<br>"
            + $"Colors: {colorStr}<br>"
            + $"Type: {e.TextType}";
        })
        .ToArray();

      // Create separate traces for each color to enable legend
      var tracesByColor = colorKeywords
        .Select((color, index) => new { Color = color, Index = index })
        .GroupBy(x => x.Color)
        .Select(group =>
        {
          var indices = group.Select(x => x.Index).ToArray();
          var x = indices.Select(i => xValues[i]).ToArray();
          var y = indices.Select(i => yValues[i]).ToArray();
          var s = indices.Select(i => (int)sizes[i]).ToArray();
          var h = indices.Select(i => hoverText[i]).ToArray();

          return CSharpChart
            .Point<float, float, string>(x: x, y: y, MultiText: h)
            .WithTraceInfo(Name: GetColorName(group.Key), ShowLegend: true)
            .WithMarkerStyle(
              Color: Plotly.NET.Color.fromKeyword(group.Key),
              MultiSize: s,
              Opacity: config.MarkerOpacity,
              Outline: Line.init(
                Width: 0.5,
                Color: Plotly.NET.Color.fromKeyword(ColorKeyword.White)
              )
            );
        })
        .ToList();

      var sizeMetricName = config.SizingMetric == SizeMetric.Cmc ? "CMC" : "Price";

      // Combine all traces into a single chart
      var chart = CSharpChart
        .Combine(tracesByColor)
        .WithXAxisStyle(Title.init("UMAP Component 1"))
        .WithYAxisStyle(Title.init("UMAP Component 2"))
        .WithTitle(
          $"Enhanced UMAP Visualization: Card Embeddings by Color & {sizeMetricName} ({filtered.Count} cards)"
        )
        .WithSize(4096, 2160);

      logger?.LogInformation("Enhanced UMAP scatter plot generated successfully");

      return await Task.FromResult(chart);
    };
  }

  /// <summary>
  /// Maps a card's color identity to a visualization color.
  /// </summary>
  /// <remarks>
  /// For multicolor cards, randomly selects one of the constituent colors.
  /// For colorless cards, returns gray.
  /// </remarks>
  private static ColorKeyword GetColorForCard(List<ManaColor>? colorIdentity, Random random)
  {
    if (colorIdentity == null || colorIdentity.Count == 0)
    {
      return ColorKeyword.Gray;
    }

    // For multicolor, randomly pick one
    var selectedColor = colorIdentity[random.Next(colorIdentity.Count)];

    return selectedColor switch
    {
      ManaColor.White => ColorKeyword.Yellow,
      ManaColor.Blue => ColorKeyword.DodgerBlue,
      ManaColor.Black => ColorKeyword.Black,
      ManaColor.Red => ColorKeyword.Crimson,
      ManaColor.Green => ColorKeyword.ForestGreen,
      _ => ColorKeyword.DarkGray,
    };
  }

  /// <summary>
  /// Gets a display name for a color keyword for legend labels.
  /// </summary>
  private static string GetColorName(ColorKeyword color)
  {
    if (color == ColorKeyword.Yellow)
      return "White";
    if (color == ColorKeyword.DodgerBlue)
      return "Blue";
    if (color == ColorKeyword.Black)
      return "Black";
    if (color == ColorKeyword.Crimson)
      return "Red";
    if (color == ColorKeyword.ForestGreen)
      return "Green";
    if (color == ColorKeyword.DarkGray)
      return "Colorless";
    return color.ToString();
  }

  /// <summary>
  /// Formats color identity for display in hover text.
  /// </summary>
  private static string FormatColorIdentity(List<ManaColor>? colorIdentity)
  {
    if (colorIdentity == null || colorIdentity.Count == 0)
    {
      return "Colorless";
    }

    var colorSymbols = colorIdentity.Select(c =>
      c switch
      {
        ManaColor.White => "W",
        ManaColor.Blue => "U",
        ManaColor.Black => "B",
        ManaColor.Red => "R",
        ManaColor.Green => "G",
        _ => "?",
      }
    );

    return string.Join("", colorSymbols);
  }

  /// <summary>
  /// Truncates text to a maximum length for hover display.
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
