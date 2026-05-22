using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Per-line cluster assignment from the Clustering flow. One row per <see cref="OracleLine"/>;
/// joinable against <see cref="AtlasPoint"/> on <see cref="LineId"/>. The
/// <see cref="ClusterId"/> of <c>-1</c> is HDBSCAN's reserved "noise" label for points the
/// clusterer couldn't confidently assign.
/// </summary>
[FlowthruSchema]
public partial record ClusterAssignment
{
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  /// <summary><c>-1</c> = noise (HDBSCAN convention); otherwise a non-negative cluster index.</summary>
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }
}
