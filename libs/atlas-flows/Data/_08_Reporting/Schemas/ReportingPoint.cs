using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Model-agnostic 2D point used by the Reporting flow. A projection of
/// <see cref="MagicAtlas.Data._03_Primary.Schemas.AtlasPoint"/> with no transformation today —
/// anything that can produce <c>(line_id, x, y)</c> can be rendered. Card metadata is reached
/// by joining <c>line_id → OracleLine.card_id → CardHoverInfo</c> downstream in the Plotly
/// step.
/// </summary>
[FlowthruSchema]
public partial record ReportingPoint
{
  /// <summary>Join key shared with cluster assignments + labels.</summary>
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  [SerializedLabel("x")]
  public required double X { get; init; }

  [SerializedLabel("y")]
  public required double Y { get; init; }
}
