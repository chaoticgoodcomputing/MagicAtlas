using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Per-point cluster assignment from the Clustering flow. One row per fragment; joinable against
/// <see cref="AtlasPoint"/> / <see cref="OracleInput"/> on <see cref="PointId"/>. The
/// <see cref="ClusterId"/> of <c>-1</c> is HDBSCAN's reserved "noise" label for points the
/// clusterer couldn't confidently assign to any cluster.
/// </summary>
[FlowthruSchema]
public partial record ClusterAssignment
{
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  /// <summary><c>-1</c> = noise (HDBSCAN convention); otherwise a non-negative cluster index.</summary>
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }
}
