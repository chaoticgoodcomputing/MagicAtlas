using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

/// <summary>
/// Performs K-means clustering on oracle text embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Uses ML.NET's KMeansTrainer to cluster 384-dimensional sentence embeddings.
/// Each oracle text entry is assigned to the nearest cluster centroid.
/// </para>
/// <para>
/// <strong>Algorithm:</strong> K-means++ initialization (default) with squared Euclidean distance
/// </para>
/// <para>
/// <strong>Outputs:</strong>
/// </para>
/// <list type="number">
/// <item>Cluster assignments: TextEntryId → ClusterId + distance to centroid</item>
/// <item>Cluster metadata: Per-cluster statistics (mean, std dev, representative observation)</item>
/// </list>
/// </remarks>
public static class KMeansClusteringNode
{
  /// <summary>
  /// Configuration options for K-means clustering.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of clusters to create (K in K-means).
    /// </summary>
    /// <remarks>
    /// Default: 32 clusters
    /// </remarks>
    public int NumberOfClusters { get; init; } = 32;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    public int? Seed { get; init; } = 42;
  }

  /// <summary>
  /// Creates a K-means clustering node with specified options.
  /// </summary>
  /// <param name="options">Configuration options for clustering</param>
  /// <returns>
  /// Transform function that takes embeddings and returns (assignments, metadata)
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<(IEnumerable<OracleTextClusterAssignment>, KMeansClusterMetadata)>
  > Create(Params? options = null)
  {
    var opts = options ?? new Params();

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();
      Console.WriteLine(
        $"Clustering {embeddingsList.Count} oracle text embeddings into {opts.NumberOfClusters} clusters..."
      );

      // Create ML.NET context
      var mlContext = new MLContext(seed: opts.Seed);

      // Convert embeddings to ML.NET format
      var trainingData = embeddingsList.Select(e => new ClusteringInput { Features = e.Embedding });

      var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

      // Configure K-means trainer
      var pipeline = mlContext.Clustering.Trainers.KMeans(
        featureColumnName: "Features",
        numberOfClusters: opts.NumberOfClusters
      );

      // Train the model
      Console.WriteLine("Training K-means model...");
      var model = pipeline.Fit(dataView);

      // Create prediction engine
      var predictionEngine = mlContext.Model.CreatePredictionEngine<
        ClusteringInput,
        ClusteringOutput
      >(model);

      // Generate predictions for all embeddings
      Console.WriteLine("Generating cluster assignments...");
      var predictions = embeddingsList
        .Select(e =>
        {
          var input = new ClusteringInput { Features = e.Embedding };
          var output = predictionEngine.Predict(input);
          return (Embedding: e, Prediction: output);
        })
        .ToList();

      // Create cluster assignments
      var assignments = predictions
        .Select(p =>
        {
          // ML.NET K-means: Score array contains distances to all clusters
          // PredictedClusterId is 1-indexed, but the Score array is 0-indexed
          // So we need to subtract 1 to get the correct array index
          var clusterIndex = (int)p.Prediction.PredictedClusterId - 1;
          var distanceToCluster = p.Prediction.Distances?[clusterIndex] ?? 0f;

          return new OracleTextClusterAssignment
          {
            TextEntryId = p.Embedding.TextEntryId,
            CardId = p.Embedding.CardId,
            ClusterId = p.Prediction.PredictedClusterId,
            DistanceToCluster = distanceToCluster,
          };
        })
        .ToList();

      // Calculate cluster metadata
      Console.WriteLine("Computing cluster statistics...");
      var metadata = ComputeClusterMetadata(assignments, opts.NumberOfClusters);

      Console.WriteLine(
        $"Clustering complete: {metadata.TotalObservations} observations across {metadata.NumberOfClusters} clusters"
      );

      return await Task.FromResult((assignments.AsEnumerable(), metadata));
    };
  }

  /// <summary>
  /// Computes statistical metadata for each cluster.
  /// </summary>
  private static KMeansClusterMetadata ComputeClusterMetadata(
    List<OracleTextClusterAssignment> assignments,
    int numberOfClusters
  )
  {
    var clusterGroups = assignments.GroupBy(a => a.ClusterId).OrderBy(g => g.Key).ToList();

    var clusterSummaries = new List<ClusterSummary>();

    foreach (var group in clusterGroups)
    {
      var distances = group.Select(a => a.DistanceToCluster).ToList();
      var count = distances.Count;

      // Calculate mean
      var mean = distances.Average();

      // Calculate standard deviation
      var variance = distances.Sum(d => Math.Pow(d - mean, 2)) / count;
      var stdDev = (float)Math.Sqrt(variance);

      clusterSummaries.Add(
        new ClusterSummary
        {
          ClusterId = group.Key,
          ObservationCount = count,
          MeanDistance = mean,
          StdDevDistance = stdDev,
        }
      );
    }

    return new KMeansClusterMetadata
    {
      NumberOfClusters = numberOfClusters,
      TotalObservations = assignments.Count,
      Clusters = clusterSummaries,
    };
  }

  /// <summary>
  /// ML.NET input schema for clustering.
  /// </summary>
  private class ClusteringInput
  {
    [VectorType(384)]
    public float[] Features { get; set; } = null!;
  }

  /// <summary>
  /// ML.NET output schema for clustering predictions.
  /// </summary>
  private class ClusteringOutput
  {
    [ColumnName("PredictedLabel")]
    public uint PredictedClusterId { get; set; }

    [ColumnName("Score")]
    public float[]? Distances { get; set; }
  }
}
