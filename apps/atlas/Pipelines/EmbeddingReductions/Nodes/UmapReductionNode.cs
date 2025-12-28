using Flowthru.Extensions.ML.UMAP;
using Flowthru.Extensions.ML.UMAP.Core;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.ML;

namespace MagicAtlas.Pipelines.EmbeddingReductions.Nodes;

/// <summary>
/// Performs UMAP dimensionality reduction on oracle text embeddings.
/// </summary>
/// <remarks>
/// <para>
/// Uses UMAP (Uniform Manifold Approximation and Projection) to reduce 384-dimensional
/// sentence embeddings to a lower-dimensional representation while preserving both
/// local and global structure of the data manifold.
/// </para>
/// <para>
/// <strong>Algorithm:</strong> UMAP - A manifold learning technique that constructs
/// a high-dimensional graph representation and optimizes a low-dimensional layout
/// to preserve topological structure.
/// </para>
/// <para>
/// <strong>Advantages over PCA:</strong>
/// </para>
/// <list type="bullet">
/// <item>Preserves local neighborhood structure (semantic similarity)</item>
/// <item>Maintains global topological relationships</item>
/// <item>Captures non-linear patterns in embeddings</item>
/// <item>Produces better visual separation for clusters</item>
/// </list>
/// <para>
/// <strong>Output:</strong> Lower-dimensional embeddings optimized for visualization
/// and cluster analysis.
/// </para>
/// <para>
/// <strong>Citation:</strong> McInnes, L, Healy, J, "UMAP: Uniform Manifold Approximation
/// and Projection for Dimension Reduction", ArXiv e-prints 1802.03426, 2018
/// https://arxiv.org/abs/1802.03426
/// </para>
/// </remarks>
public static class UmapReductionNode
{
  /// <summary>
  /// Configuration options for UMAP dimensionality reduction.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of neighboring points used in local approximations of manifold structure.
    /// </summary>
    /// <remarks>
    /// <para>Default: 15</para>
    /// <para>Range: 2-100</para>
    /// <para>
    /// Larger values result in more global structure being preserved at the loss of
    /// detailed local structure. Smaller values focus on local neighborhoods.
    /// </para>
    /// <para>Common values: 5-50 depending on dataset size and structure.</para>
    /// </remarks>
    public int NumberOfNeighbors { get; init; } = 100;

    /// <summary>
    /// Target dimensionality of the embedding space.
    /// </summary>
    /// <remarks>
    /// <para>Default: 2 (for 2D visualization)</para>
    /// <para>Common values:</para>
    /// <list type="bullet">
    /// <item>2: 2D scatter plots and visualizations</item>
    /// <item>3: 3D interactive visualizations</item>
    /// </list>
    /// </remarks>
    public int NumberOfComponents { get; init; } = 2;

    /// <summary>
    /// Minimum distance between points in the low-dimensional embedding.
    /// </summary>
    /// <remarks>
    /// <para>Default: 0.1</para>
    /// <para>Range: 0.0-0.5</para>
    /// <para>
    /// Controls how tightly the embedding compresses points together. Smaller values
    /// allow more accurate local structure preservation. Larger values produce more
    /// evenly distributed embeddings.
    /// </para>
    /// </remarks>
    public float MinDist { get; init; } = 0.25f;

    /// <summary>
    /// Distance metric for measuring similarity in high-dimensional space.
    /// </summary>
    /// <remarks>
    /// <para>Default: "euclidean"</para>
    /// <para>Supported metrics:</para>
    /// <list type="bullet">
    /// <item>"euclidean": Standard L2 distance</item>
    /// <item>"cosine": Cosine similarity (good for embeddings)</item>
    /// <item>"correlation": Pearson correlation distance</item>
    /// <item>"manhattan": L1 distance</item>
    /// </list>
    /// <para>
    /// For sentence embeddings, "cosine" is often preferred as it focuses on
    /// directional similarity rather than magnitude.
    /// </para>
    /// </remarks>
    public string Metric { get; init; } = "euclidean";

    /// <summary>
    /// Number of training epochs for optimization.
    /// </summary>
    /// <remarks>
    /// <para>Default: null (auto-selected based on dataset size)</para>
    /// <para>
    /// When null, UMAP selects 500 epochs for small datasets (&lt;10k samples)
    /// and 200 epochs for large datasets.
    /// </para>
    /// <para>More epochs result in better optimization but longer training time.</para>
    /// </remarks>
    public int? NumberOfEpochs { get; init; } = null;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    /// <remarks>
    /// Set to a fixed value for reproducible results across runs.
    /// </remarks>
    public int? Seed { get; init; } = 42;

    /// <summary>
    /// Verbosity level for progress reporting.
    /// </summary>
    /// <remarks>
    /// <para>0 = Silent (no progress output)</para>
    /// <para>1 = Minimal (major phases only)</para>
    /// <para>2 = Detailed (phase progress percentages)</para>
    /// <para>Default is 2 for pipeline visibility.</para>
    /// </remarks>
    public int Verbosity { get; init; } = 2;

    /// <summary>
    /// Standard deviation of Gaussian noise added to embeddings before UMAP.
    /// </summary>
    /// <remarks>
    /// <para>Default: 1e-5 (0.00001)</para>
    /// <para>Range: 0.0-0.01</para>
    /// <para>
    /// Small amounts of noise help differentiate identical or near-identical embeddings,
    /// which can cause issues with K-NN search. The noise is applied element-wise to
    /// each dimension of the embedding vectors.
    /// </para>
    /// <para>
    /// Set to 0.0 to disable noise injection. Typical values range from 1e-6 to 1e-4
    /// depending on the embedding scale and degree of duplication.
    /// </para>
    /// </remarks>
    public float NoiseScale { get; init; } = 1e-3f;
  }

  /// <summary>
  /// Creates a UMAP reduction node with specified options.
  /// </summary>
  /// <param name="options">Configuration options for UMAP</param>
  /// <returns>
  /// Transform function that takes embeddings and returns UMAP-reduced embeddings
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<OracleUmapEmbedding>>
  > Create(Params? options = null)
  {
    var opts = options ?? new Params();

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();

      // Create ML.NET context
      var mlContext = new MLContext(seed: opts.Seed);

      // Extract embedding vectors as float[][]
      var data = embeddingsList.Select(e => e.Embedding).ToArray();

      // Apply noise to break ties in identical/near-identical embeddings
      if (opts.NoiseScale > 0)
      {
        ApplyGaussianNoise(data, opts.NoiseScale, opts.Seed ?? 42);
      }

      // Configure UMAP options
      var umapParameters = new UmapParameters
      {
        NumberOfNeighbors = 50,
        LearningRate = 1.0f,
        MinDist = 0.75f,
        NumberOfComponents = 2,
        RandomSeed = 43,
        NumberOfEpochs = 500,
        Verbosity = 2,
      };

      // Use simplified high-level API with specified initialization strategy
      var embeddingMatrix = UmapPipeline.Create(umapParameters).FitTransform(data);

      // Generate UMAP embeddings
      var umapEmbeddings = embeddingsList
        .Select(
          (e, i) =>
            new OracleUmapEmbedding
            {
              TextEntryId = e.TextEntryId,
              CardId = e.CardId,
              TextType = e.TextType,
              Text = e.Text,
              Components = embeddingMatrix[i],
              ComponentDimension = opts.NumberOfComponents,
            }
        )
        .ToList();

      return await Task.FromResult(umapEmbeddings.AsEnumerable());
    };
  }

  /// <summary>
  /// Applies Gaussian noise to an embedding matrix in-place.
  /// </summary>
  /// <param name="embeddings">The embedding matrix to modify (float[][])</param>
  /// <param name="noiseScale">Standard deviation of the Gaussian noise</param>
  /// <param name="seed">Random seed for reproducibility</param>
  /// <remarks>
  /// Adds element-wise Gaussian noise N(0, noiseScale²) to each dimension.
  /// This helps differentiate identical embeddings for K-NN algorithms.
  /// </remarks>
  private static void ApplyGaussianNoise(float[][] embeddings, float noiseScale, int seed)
  {
    var random = new Random(seed);

    for (int i = 0; i < embeddings.Length; i++)
    {
      var embedding = embeddings[i];
      for (int j = 0; j < embedding.Length; j++)
      {
        // Box-Muller transform for Gaussian noise
        double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] - avoid log(0)
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        embedding[j] += (float)(randStdNormal * noiseScale);
      }
    }
  }
}
