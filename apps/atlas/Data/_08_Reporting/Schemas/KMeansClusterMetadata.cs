using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Statistical metadata for K-means clustering of oracle text embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Contains aggregate statistics for each cluster discovered by K-means algorithm.
/// Provides insights into cluster cohesion and identifies representative observations.
/// </para>
/// <para>
/// <strong>Distance Metric:</strong> All distance values are squared Euclidean distances
/// </para>
/// </remarks>
public record KMeansClusterMetadata : IStructuredSerializable
{
  /// <summary>
  /// Total number of clusters in the K-means model.
  /// </summary>
  public required int NumberOfClusters { get; init; }

  /// <summary>
  /// Total number of observations (oracle text embeddings) clustered.
  /// </summary>
  public required int TotalObservations { get; init; }

  /// <summary>
  /// Per-cluster statistical summaries.
  /// </summary>
  public required List<ClusterSummary> Clusters { get; init; }
}

/// <summary>
/// Statistical summary for a single cluster.
/// </summary>
public record ClusterSummary
{
  /// <summary>
  /// Cluster ID (0-indexed).
  /// </summary>
  public required uint ClusterId { get; init; }

  /// <summary>
  /// Number of observations assigned to this cluster.
  /// </summary>
  public required int ObservationCount { get; init; }

  /// <summary>
  /// Mean of squared Euclidean distances to centroid for observations in this cluster.
  /// </summary>
  /// <remarks>
  /// Lower values indicate the cluster is more cohesive (observations are closer to centroid).
  /// </remarks>
  public required float MeanDistance { get; init; }

  /// <summary>
  /// Standard deviation of squared Euclidean distances within this cluster.
  /// </summary>
  /// <remarks>
  /// Lower values indicate more uniform distances (tighter cluster).
  /// Higher values suggest the cluster contains more spread-out observations.
  /// </remarks>
  public required float StdDevDistance { get; init; }
}
