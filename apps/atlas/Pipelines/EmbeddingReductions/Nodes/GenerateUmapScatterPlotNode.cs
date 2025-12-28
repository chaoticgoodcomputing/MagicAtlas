using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data.Enums.Card;
using Microsoft.Extensions.Logging;
using Plotly.NET;
using Plotly.NET.LayoutObjects;
using Plotly.NET.TraceObjects;
using CSharpChart = Plotly.NET.CSharp.Chart;

namespace MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

/// <summary>
/// Generates a 2D scatter plot of UMAP-reduced embeddings, colored by oracle text type.
/// Filters out specified text types before visualization.
/// </summary>
/// <remarks>
/// <para>
/// Creates a scatter plot visualization to explore the UMAP-reduced embedding space.
/// Each point represents one oracle text entry, positioned by its UMAP coordinates
/// and colored according to its text type (keyword ability, triggered ability, etc.).
/// </para>
/// <para>
/// <strong>Input:</strong> UMAP-reduced embeddings (typically 2D)
/// </para>
/// <para>
/// <strong>Output:</strong> GenericChart with color-coded scatter traces by text type
/// </para>
/// <para>
/// <strong>Visualization Purpose:</strong>
/// </para>
/// <list type="bullet">
/// <item>Explore clustering patterns in the reduced embedding space</item>
/// <item>Identify if different text types occupy distinct regions</item>
/// <item>Validate UMAP dimension reduction effectiveness</item>
/// <item>Compare with PCA to see non-linear structure preservation</item>
/// </list>
/// </remarks>
public static class GenerateUmapScatterPlotNode
{
  /// <summary>
  /// Configuration options for the scatter plot.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Text types to exclude from visualization.
    /// Default: excludes Full text entries to focus on individual abilities.
    /// </summary>
    public HashSet<OracleTextType> ExcludedTextTypes { get; init; } = new() { OracleTextType.Full };
  }

  /// <summary>
  /// Creates a scatter plot generation function.
  /// </summary>
  /// <param name="options">Configuration for filtering and visualization.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// A function that generates a UMAP scatter plot from UMAP embeddings.
  /// </returns>
  public static Func<IEnumerable<OracleUmapEmbedding>, Task<GenericChart>> Create(
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
          "Cannot generate scatter plot from empty embedding dataset"
        );
      }

      // Filter out excluded text types
      var filtered = embeddings.Where(e => !config.ExcludedTextTypes.Contains(e.TextType)).ToList();

      logger?.LogInformation(
        "Generating UMAP scatter plot: {TotalCount} embeddings → {FilteredCount} after filtering (excluded {ExcludedCount})",
        embeddings.Count,
        filtered.Count,
        embeddings.Count - filtered.Count
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

      // Group embeddings by text type for color-coding
      var groupedByType = filtered.GroupBy(e => e.TextType).OrderBy(g => g.Key).ToList();

      logger?.LogInformation(
        "Creating scatter traces for {TypeCount} text types",
        groupedByType.Count
      );

      // Create a scatter trace for each text type
      var traces = groupedByType.Select(group => CreateScatterTrace(group.Key, group.ToList()));

      // Combine all traces into a single chart
      var chart = CSharpChart
        .Combine(traces)
        .WithXAxisStyle(Title.init("UMAP Component 1"))
        .WithYAxisStyle(Title.init("UMAP Component 2"))
        .WithTitle(
          $"UMAP Scatter Plot: Oracle Text Embeddings ({filtered.Count} observations, {componentDim} components)"
        )
        .WithSize(1920, 1080);

      logger?.LogInformation("UMAP scatter plot generated successfully");

      return await Task.FromResult(chart);
    };
  }

  /// <summary>
  /// Creates a scatter trace for a specific text type.
  /// </summary>
  private static GenericChart CreateScatterTrace(
    OracleTextType textType,
    List<OracleUmapEmbedding> embeddings
  )
  {
    // Extract first two UMAP components for X and Y coordinates
    var xValues = embeddings.Select(e => e.Components[0]).ToArray();
    var yValues = embeddings.Select(e => e.Components[1]).ToArray();

    // Create hover text with card info
    var hoverText = embeddings
      .Select(e => $"Card: {e.CardId}<br>Type: {textType}<br>Text: {TruncateText(e.Text, 100)}")
      .ToArray();

    return CSharpChart
      .Point<float, float, string>(x: xValues, y: yValues, MultiText: hoverText)
      .WithTraceInfo(Name: GetTextTypeName(textType), ShowLegend: true)
      .WithMarkerStyle(
        Color: Plotly.NET.Color.fromKeyword(GetColorForTextType(textType)),
        Size: 3,
        Opacity: 0.7
      );
  }

  /// <summary>
  /// Gets a display-friendly name for each text type.
  /// </summary>
  private static string GetTextTypeName(OracleTextType textType)
  {
    return textType switch
    {
      OracleTextType.Full => "Full Text",
      OracleTextType.KeywordAbility => "Keyword Ability",
      OracleTextType.NamedTriggeredAbility => "Named Triggered",
      OracleTextType.ActivatedAbility => "Activated",
      OracleTextType.TriggeredAbility => "Triggered",
      OracleTextType.PassiveAbility => "Passive",
      _ => textType.ToString(),
    };
  }

  /// <summary>
  /// Assigns a distinct color to each text type for visualization.
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
