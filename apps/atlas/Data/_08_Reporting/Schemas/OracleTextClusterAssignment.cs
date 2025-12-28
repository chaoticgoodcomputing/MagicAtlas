using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Maps oracle text embeddings to K-means cluster assignments.
/// </summary>
/// <remarks>
/// <para>
/// Each row represents a single oracle text entry's cluster assignment
/// from K-means clustering on the 384-dimensional embedding vectors.
/// </para>
/// <para>
/// <strong>Distance Metric:</strong> Squared Euclidean distance to assigned cluster centroid
/// </para>
/// </remarks>
public record OracleTextClusterAssignment : IFlatSchema, ITextSerializable
{
  /// <summary>
  /// Unique identifier for the oracle text entry.
  /// References the TextEntryId from OracleTextEmbedding.
  /// </summary>
  [SerializedLabel("text_entry_id")]
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Scryfall card ID associated with this text entry.
  /// </summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>
  /// Assigned cluster ID (0-indexed).
  /// </summary>
  /// <remarks>
  /// The cluster assignment is determined by finding the centroid with
  /// minimum squared Euclidean distance to the embedding vector.
  /// </remarks>
  [SerializedLabel("cluster_id")]
  public required uint ClusterId { get; init; }

  /// <summary>
  /// Squared Euclidean distance from the embedding to the assigned cluster's centroid.
  /// </summary>
  /// <remarks>
  /// This is the Score[PredictedLabel] value from ML.NET K-means output.
  /// Lower values indicate the observation is closer to its cluster centroid.
  /// </remarks>
  [SerializedLabel("distance_to_cluster")]
  public required float DistanceToCluster { get; init; }
}
