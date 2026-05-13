using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A 2D UMAP coordinate for a single oracle-text fragment. One card can produce multiple points
/// (one per ability: keyword, triggered, activated, etc.) — <see cref="CardId"/> is the link back
/// to the owning card but is NOT unique across rows.
/// </summary>
[FlowthruSchema]
public partial record AtlasPoint
{
  /// <summary>
  /// Globally-unique fragment identifier — the join key for cluster assignments and labels.
  /// Sourced from the upstream <c>OracleInput.PointId</c>, preserved through embedding + reduction.
  /// </summary>
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("x")]
  public required double X { get; init; }

  [SerializedLabel("y")]
  public required double Y { get; init; }

  /// <summary>keyword | named_triggered | triggered | activated | passive</summary>
  [SerializedLabel("text_type")]
  public required string TextType { get; init; }
}
