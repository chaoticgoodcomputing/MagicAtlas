using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A 2D UMAP coordinate for a single <see cref="OracleLine"/>. One card can produce multiple
/// points (one per ability line). Card metadata is reached via the <see cref="OracleLine"/>
/// join — this row carries only the coordinate pair plus the line key.
/// </summary>
[FlowthruSchema]
public partial record AtlasPoint
{
  /// <summary>
  /// The join key — matches <c>OracleLine.LineId</c>. Card metadata and the original text are
  /// reached by joining back to <c>OracleLines</c>.
  /// </summary>
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  [SerializedLabel("x")]
  public required double X { get; init; }

  [SerializedLabel("y")]
  public required double Y { get; init; }
}
