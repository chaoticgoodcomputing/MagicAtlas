using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

/// <summary>
/// Enriches K-means cluster metadata with card information for interpretability.
/// </summary>
/// <remarks>
/// <para>
/// Joins cluster metadata with card core data and metadata to provide context about
/// what each cluster represents through its most representative card.
/// </para>
/// </remarks>
public static class EnrichClusterMetadataNode
{
  /// <summary>
  /// Configuration parameters for the enrichment node.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of closest observations to include per cluster. Default: 5
    /// </summary>
    public int TopN { get; init; } = 5;
  }

  /// <summary>
  /// Creates a node that enriches cluster metadata with card information using the
  /// cluster assignments table to select the N closest observations per cluster.
  /// </summary>
  /// <returns>
  /// Transform function that takes (metadata, assignments, card data, card metadata) and returns enriched metadata
  /// </returns>
  public static Func<
    (
      KMeansClusterMetadata metadata,
      IEnumerable<OracleTextClusterAssignment> assignments,
      IEnumerable<CardCoreData> coreData,
      IEnumerable<CardMetadata> cardMetadata
    ),
    Task<EnrichedClusterMetadata>
  > Create(Params? @params = null)
  {
    var opts = @params ?? new Params();

    return async (input) =>
    {
      var (clusterMetadata, assignments, coreData, metadata) = input;

      Console.WriteLine(
        $"Enriching cluster metadata with card information for {clusterMetadata.NumberOfClusters} clusters (top {opts.TopN} per cluster)..."
      );

      // Build lookups for efficient joins
      var coreDataLookup = coreData.ToDictionary(c => c.Id);
      var metadataLookup = metadata.ToDictionary(m => m.Id);

      // Group assignments by cluster for fast access
      var assignmentsByCluster = assignments
        .GroupBy(a => a.ClusterId)
        .ToDictionary(g => g.Key, g => g.OrderBy(a => a.DistanceToCluster).ToList());

      var enrichedClusters = new List<EnrichedClusterSummary>();

      foreach (var cluster in clusterMetadata.Clusters)
      {
        // Pull assignments for this cluster (if any)
        if (!assignmentsByCluster.TryGetValue(cluster.ClusterId, out var clusterAssignments))
        {
          Console.WriteLine($"Warning: No assignments found for cluster {cluster.ClusterId}");
          continue;
        }

        var closest = clusterAssignments
          .Take(opts.TopN)
          .Select(
            (a, idx) =>
            {
              // join card info if available
              coreDataLookup.TryGetValue(a.CardId, out var cardCore);
              metadataLookup.TryGetValue(a.CardId, out var cardMeta);

              var oracleTextLines = cardCore
                ?.OracleText?.Split('\n')
                .Select(line => line.Trim())
                .ToList();

              return new ClosestObservation
              {
                CardId = a.CardId,
                TextEntryId = a.TextEntryId,
                DistanceToCluster = a.DistanceToCluster,
                Rank = idx + 1,
                Name = cardCore?.Name ?? string.Empty,
                ManaCost = cardCore?.ManaCost,
                OracleText = oracleTextLines,
                ScryfallUri = cardMeta?.ScryfallUri ?? string.Empty,
                PriceUsd = cardMeta?.Prices?.Usd,
              };
            }
          )
          .ToList();

        enrichedClusters.Add(
          new EnrichedClusterSummary
          {
            ClusterId = cluster.ClusterId,
            ObservationCount = cluster.ObservationCount,
            MeanDistance = cluster.MeanDistance,
            StdDevDistance = cluster.StdDevDistance,
            ClosestObservations = closest,
          }
        );
      }

      var enrichedMetadata = new EnrichedClusterMetadata
      {
        NumberOfClusters = clusterMetadata.NumberOfClusters,
        TotalObservations = clusterMetadata.TotalObservations,
        Clusters = enrichedClusters,
      };

      Console.WriteLine(
        $"Successfully enriched {enrichedClusters.Count}/{clusterMetadata.NumberOfClusters} clusters with top-{opts.TopN} observations"
      );

      return await Task.FromResult(enrichedMetadata);
    };
  }
}
