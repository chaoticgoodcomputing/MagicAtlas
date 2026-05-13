using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Model-agnostic 2D point used by the Reporting flow. A projection of
/// <see cref="MagicAtlas.Data._03_Primary.Schemas.AtlasPoint"/> with the embedding-specific
/// <c>text_type</c> field stripped — anything that can produce <c>(card_id, x, y)</c> can be
/// rendered, regardless of how the upstream embedding model classified its inputs. This is the
/// shape we'd compare across different embedding models / fine-tunings.
/// </summary>
[FlowthruSchema]
public partial record ReportingPoint
{
  /// <summary>
  /// Join key shared with cluster assignments + labels. Sourced from <c>AtlasPoint.PointId</c>.
  /// </summary>
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("x")]
  public required double X { get; init; }

  [SerializedLabel("y")]
  public required double Y { get; init; }
}
