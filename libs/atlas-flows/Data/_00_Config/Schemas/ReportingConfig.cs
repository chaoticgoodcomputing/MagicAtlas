using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the Reporting flow — Plotly display knobs consumed by
/// <c>build_atlas_plot.py</c>. Materialized at startup from <c>Flowthru:Flows:Reporting</c> in
/// <c>appsettings.json</c>.
/// </summary>
/// <remarks>
/// Aesthetic constants that aren't really tuned (color palette, font sizes, padding) stay in
/// the Python source. Only knobs that meaningfully change the output legibility live here.
/// </remarks>
[FlowthruSchema]
public partial record ReportingConfig
{
  /// <summary>Top-N largest clusters that get a centroid text annotation.</summary>
  public required int MaxAnnotations { get; init; }

  /// <summary>Character cap on each centroid annotation before ellipsis.</summary>
  public required int AnnotationTextLimit { get; init; }

  /// <summary>Scatter marker size in pixels.</summary>
  public required int MarkerSize { get; init; }

  /// <summary>Scatter marker opacity (0–1).</summary>
  public required double MarkerOpacity { get; init; }

  /// <summary>Character cap on oracle text shown in the hover tooltip.</summary>
  public required int OracleHoverTruncateLimit { get; init; }
}
