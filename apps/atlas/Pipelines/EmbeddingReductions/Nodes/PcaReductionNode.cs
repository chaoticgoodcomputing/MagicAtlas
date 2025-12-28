using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

/// <summary>
/// Performs PCA dimensionality reduction on oracle text embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Uses ML.NET's PrincipalComponentAnalysisTransformer to reduce 384-dimensional
/// sentence embeddings to a lower-dimensional representation while preserving
/// the most important variance.
/// </para>
/// <para>
/// <strong>Algorithm:</strong> Principal Component Analysis (PCA) with configurable
/// number of components
/// </para>
/// <para>
/// <strong>Output:</strong> Lower-dimensional embeddings that retain the most
/// significant variance from the original high-dimensional space.
/// </para>
/// </remarks>
public static class PcaReductionNode
{
  /// <summary>
  /// Configuration options for PCA dimensionality reduction.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of principal components to retain.
    /// </summary>
    /// <remarks>
    /// Default: 50 components (from 384 dimensions)
    /// Common values: 2-3 for visualization, 10-100 for downstream ML
    /// </remarks>
    public int Rank { get; init; } = 50;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    public int? Seed { get; init; } = 42;

    /// <summary>
    /// Whether to center the data (subtract mean).
    /// </summary>
    /// <remarks>
    /// Default: true (standard PCA practice)
    /// </remarks>
    public bool EnsureZeroMean { get; init; } = true;
  }

  /// <summary>
  /// Creates a PCA reduction node with specified options.
  /// </summary>
  /// <param name="options">Configuration options for PCA</param>
  /// <returns>
  /// Transform function that takes embeddings and returns PCA-reduced embeddings
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<OraclePcaEmbedding>>
  > Create(Params? options = null)
  {
    var opts = options ?? new Params();

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();
      Console.WriteLine(
        $"Performing PCA reduction on {embeddingsList.Count} embeddings from 384 to {opts.Rank} dimensions..."
      );

      // Create ML.NET context
      var mlContext = new MLContext(seed: opts.Seed);

      // Convert embeddings to ML.NET format
      var trainingData = embeddingsList.Select(e => new PcaInput { Features = e.Embedding });

      var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

      // Configure PCA transformer
      var pipeline = mlContext.Transforms.ProjectToPrincipalComponents(
        outputColumnName: "PcaFeatures",
        inputColumnName: "Features",
        rank: opts.Rank,
        ensureZeroMean: opts.EnsureZeroMean,
        seed: opts.Seed
      );

      // Fit the PCA model
      Console.WriteLine("Fitting PCA model...");
      var model = pipeline.Fit(dataView);

      // Transform the data
      Console.WriteLine("Transforming embeddings...");
      var transformedData = model.Transform(dataView);

      // Create prediction engine for individual transformations
      var predictionEngine = mlContext.Model.CreatePredictionEngine<PcaInput, PcaOutput>(model);

      // Generate PCA embeddings
      var pcaEmbeddings = embeddingsList
        .Select(e =>
        {
          var input = new PcaInput { Features = e.Embedding };
          var output = predictionEngine.Predict(input);

          return new OraclePcaEmbedding
          {
            TextEntryId = e.TextEntryId,
            CardId = e.CardId,
            TextType = e.TextType,
            Text = e.Text,
            Components = output.PcaFeatures,
            ComponentDimension = opts.Rank,
          };
        })
        .ToList();

      Console.WriteLine(
        $"PCA reduction complete: {pcaEmbeddings.Count} embeddings reduced to {opts.Rank} dimensions"
      );

      return await Task.FromResult(pcaEmbeddings.AsEnumerable());
    };
  }

  /// <summary>
  /// Input schema for PCA transformation.
  /// </summary>
  private class PcaInput
  {
    /// <summary>
    /// Original 384-dimensional embedding vector.
    /// </summary>
    [VectorType(384)]
    public float[] Features { get; set; } = null!;
  }

  /// <summary>
  /// Output schema for PCA transformation.
  /// </summary>
  private class PcaOutput
  {
    /// <summary>
    /// PCA-reduced feature vector.
    /// Dimensionality determined by the Rank parameter.
    /// </summary>
    [ColumnName("PcaFeatures")]
    public float[] PcaFeatures { get; set; } = null!;
  }
}
