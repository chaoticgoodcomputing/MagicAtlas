using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// K-means cluster metadata enriched with card information for interpretability.
/// </summary>
/// <remarks>
/// <para>
/// Combines statistical cluster metadata with actual card data from the representative
/// observation in each cluster, making it easier to understand what each cluster represents.
/// </para>
/// </remarks>
public record EnrichedClusterMetadata : IStructuredSerializable
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
  /// Per-cluster summaries enriched with card information.
  /// </summary>
  public required List<EnrichedClusterSummary> Clusters { get; init; }
}

/// <summary>
/// Statistical summary for a single cluster with its closest observations and card information.
/// </summary>
public record EnrichedClusterSummary
{
  /// <summary>
  /// Cluster ID (1-indexed).
  /// </summary>
  public required uint ClusterId { get; init; }

  /// <summary>
  /// Number of observations assigned to this cluster.
  /// </summary>
  public required int ObservationCount { get; init; }

  /// <summary>
  /// Mean of squared Euclidean distances to centroid for observations in this cluster.
  /// </summary>
  public required float MeanDistance { get; init; }

  /// <summary>
  /// Standard deviation of squared Euclidean distances within this cluster.
  /// </summary>
  public required float StdDevDistance { get; init; }

  /// <summary>
  /// Ordered list (ascending distance) of the N closest observations for this cluster.
  /// </summary>
  public required List<ClosestObservation> ClosestObservations { get; init; }
}

/// <summary>
/// Card information for a cluster's representative observation.
/// </summary>
public record ClosestObservation
{
  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  public required Guid CardId { get; init; }

  /// <summary>
  /// Associated oracle text entry id for this observation.
  /// </summary>
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Distance to the assigned cluster centroid (squared Euclidean distance).
  /// </summary>
  public required float DistanceToCluster { get; init; }

  /// <summary>
  /// 1-based rank (1 = closest)
  /// </summary>
  public required int Rank { get; init; }

  /// <summary>
  /// Name of the card.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Mana cost of the card, if any.
  /// </summary>
  public string? ManaCost { get; init; }

  /// <summary>
  /// Full oracle text of the card, split by line, if any.
  /// </summary>
  public List<string>? OracleText { get; init; }

  /// <summary>
  /// Scryfall URI for viewing the card online.
  /// </summary>
  public required string ScryfallUri { get; init; }

  /// <summary>
  /// Price in USD, if available.
  /// </summary>
  public decimal? PriceUsd { get; init; }
}
