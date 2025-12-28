using Flowthru.Extensions.ML.UMAP;
using Flowthru.Extensions.ML.UMAP.Core;
using Flowthru.Extensions.ML.UMAP.Strategies.LayoutInit.Implementations;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace MagicAtlas.Pipelines.UmapExploration.Nodes;

/// <summary>
/// Performs UMAP dimensionality reduction with multiple hyperparameter combinations.
/// </summary>
/// <remarks>
/// <para>
/// Generates a cartesian product of hyperparameter values and runs UMAP for each
/// combination, enabling systematic exploration of how UMAP hyperparameters affect
/// the embedding space structure.
/// </para>
/// <para>
/// <strong>Hyperparameters Scanned:</strong>
/// </para>
/// <list type="bullet">
/// <item><strong>NumberOfNeighbors:</strong> Controls local vs. global structure balance</item>
/// <item><strong>MinDist:</strong> Controls point spacing in low-dimensional space</item>
/// </list>
/// <para>
/// <strong>Example:</strong> With NumberOfNeighbors=[2,10,25] and MinDist=[0.01,0.1,0.5],
/// generates 3×3 = 9 UMAP embeddings for comparison.
/// </para>
/// <para>
/// <strong>Performance Note:</strong> UMAP is computationally expensive. Consider using
/// a sampled subset of embeddings for hyperparameter exploration.
/// </para>
/// </remarks>
public static class UmapHyperparameterScanNode
{
  /// <summary>
  /// Configuration options for UMAP hyperparameter scanning.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Array of NumberOfNeighbors values to explore.
    /// </summary>
    /// <remarks>
    /// <para>Required. Example: [2, 10, 25]</para>
    /// <para>
    /// Larger values emphasize global structure, smaller values emphasize local neighborhoods.
    /// Typical range: 2-100.
    /// </para>
    /// </remarks>
    public required int[] NumberOfNeighborsValues { get; init; }

    /// <summary>
    /// Array of MinDist values to explore.
    /// </summary>
    /// <remarks>
    /// <para>Required. Example: [0.01, 0.1, 0.5]</para>
    /// <para>
    /// Controls tightness of clustering. Smaller values allow tighter clusters,
    /// larger values produce more evenly distributed embeddings.
    /// Typical range: 0.0-0.99.
    /// </para>
    /// </remarks>
    public required float[] MinDistValues { get; init; }

    /// <summary>
    /// Target dimensionality for UMAP embeddings.
    /// </summary>
    /// <remarks>
    /// Default: 2 (for 2D visualization)
    /// </remarks>
    public int NumberOfComponents { get; init; } = 2;

    /// <summary>
    /// Distance metric for high-dimensional space.
    /// </summary>
    /// <remarks>
    /// Default: "euclidean". Common options: "euclidean", "cosine", "manhattan"
    /// </remarks>
    public string Metric { get; init; } = "euclidean";

    /// <summary>
    /// Number of optimization epochs.
    /// </summary>
    /// <remarks>
    /// Default: null (auto-selected by UMAP based on dataset size).
    /// More epochs = better optimization but longer runtime.
    /// </remarks>
    public int? NumberOfEpochs { get; init; } = null;

    /// <summary>
    /// Random seed for reproducibility.
    /// </summary>
    public int Seed { get; init; } = 42;

    /// <summary>
    /// Verbosity level (0=silent, 1=minimal, 2=detailed).
    /// </summary>
    public int Verbosity { get; init; } = 1;

    /// <summary>
    /// Standard deviation of Gaussian noise added to embeddings before UMAP.
    /// </summary>
    /// <remarks>
    /// Default: 1e-5. Helps differentiate identical embeddings. Set to 0.0 to disable.
    /// </remarks>
    public float NoiseScale { get; init; } = 1e-5f;
  }

  /// <summary>
  /// Creates a UMAP hyperparameter scan function.
  /// </summary>
  /// <param name="options">Configuration for hyperparameter scanning.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// Transform function that generates UMAP embeddings for all hyperparameter combinations.
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<UmapHyperparameterScanResult>>
  > Create(Params? options = null, ILogger? logger = null)
  {
    var opts = options ?? new Params { NumberOfNeighborsValues = [15], MinDistValues = [0.1f] };

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();

      if (embeddingsList.Count == 0)
      {
        throw new InvalidOperationException("Cannot perform UMAP scan on empty dataset");
      }

      logger?.LogInformation(
        "Starting UMAP hyperparameter scan: {NumNeighbors} neighbors × {NumMinDist} minDist = {Total} combinations",
        opts.NumberOfNeighborsValues.Length,
        opts.MinDistValues.Length,
        opts.NumberOfNeighborsValues.Length * opts.MinDistValues.Length
      );

      // Extract embedding vectors as float[][]
      var data = embeddingsList.Select(e => e.Embedding).ToArray();

      // Apply noise once to the shared input data
      if (opts.NoiseScale > 0)
      {
        ApplyGaussianNoise(data, opts.NoiseScale, opts.Seed);
        logger?.LogInformation(
          "Applied Gaussian noise (scale={NoiseScale}) to input embeddings",
          opts.NoiseScale
        );
      }

      var allResults = new List<UmapHyperparameterScanResult>();

      // Cartesian product of hyperparameters
      var combinations =
        from neighbors in opts.NumberOfNeighborsValues
        from minDist in opts.MinDistValues
        select new { NumberOfNeighbors = neighbors, MinDist = minDist };

      var combinationIndex = 1;
      var totalCombinations = opts.NumberOfNeighborsValues.Length * opts.MinDistValues.Length;

      Console.WriteLine("Combination plan:");
      foreach (var (i, combo) in combinations.Select((c, i) => (i + 1, c)))
      {
        Console.WriteLine(
          $"  {i} / {totalCombinations}: N = {combo.NumberOfNeighbors}, MinDist = {combo.MinDist}"
        );
      }

      foreach (var combo in combinations)
      {
        logger?.LogInformation(
          "[{Index}/{Total}] Running UMAP with NumberOfNeighbors={Neighbors}, MinDist={MinDist}",
          combinationIndex,
          totalCombinations,
          combo.NumberOfNeighbors,
          combo.MinDist
        );

        Console.WriteLine("==========");
        Console.WriteLine(
          $"{combinationIndex} / {totalCombinations}: N = {combo.NumberOfNeighbors}, MinDist = {combo.MinDist}"
        );
        Console.WriteLine("==========");

        // Use a unique seed for each combination to avoid identical state issues
        // Derive from base seed using combination index
        var combinationSeed = opts.Seed + combinationIndex;

        // Configure UMAP for this combination
        var umapParameters = new UmapParameters
        {
          NumberOfNeighbors = combo.NumberOfNeighbors,
          MinDist = combo.MinDist,
          NumberOfComponents = opts.NumberOfComponents,
          RandomSeed = combinationSeed,
          NumberOfEpochs = opts.NumberOfEpochs,
          Verbosity = opts.Verbosity,
        };

        logger?.LogInformation("  Starting UMAP FitTransform (seed={Seed})...", combinationSeed);

        // Run UMAP with RandomInit for faster hyperparameter scanning
        // Spectral init is too slow for repeated runs (O(n³) eigendecomposition)
        var embeddingMatrix = UmapPipeline
          .Create(umapParameters)
          .WithLayoutInit(new RandomInit()) // Use fast random init instead of spectral
          .FitTransform(data);

        logger?.LogInformation(
          "  UMAP FitTransform completed, generated {Rows}x{Cols} embedding matrix",
          embeddingMatrix.Length,
          embeddingMatrix.Length > 0 ? embeddingMatrix[0].Length : 0
        );

        // Convert to results with hyperparameter metadata
        var results = embeddingsList
          .Select(
            (e, i) =>
              new UmapHyperparameterScanResult
              {
                TextEntryId = e.TextEntryId,
                CardId = e.CardId,
                TextType = e.TextType,
                Text = e.Text,
                Components = embeddingMatrix[i],
                ComponentDimension = opts.NumberOfComponents,
                NumberOfNeighbors = combo.NumberOfNeighbors,
                MinDist = combo.MinDist,
                Metric = opts.Metric,
                RandomSeed = opts.Seed,
              }
          )
          .ToList();

        allResults.AddRange(results);
        combinationIndex++;
      }

      logger?.LogInformation(
        "UMAP hyperparameter scan completed: generated {Count} total embeddings across {Combinations} combinations",
        allResults.Count,
        totalCombinations
      );

      return await Task.FromResult(allResults.AsEnumerable());
    };
  }

  /// <summary>
  /// Applies Gaussian noise to an embedding matrix in-place.
  /// </summary>
  private static void ApplyGaussianNoise(float[][] embeddings, float noiseScale, int seed)
  {
    var random = new Random(seed);

    for (int i = 0; i < embeddings.Length; i++)
    {
      var embedding = embeddings[i];
      for (int j = 0; j < embedding.Length; j++)
      {
        // Box-Muller transform for Gaussian noise
        double u1 = 1.0 - random.NextDouble();
        double u2 = 1.0 - random.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        embedding[j] += (float)(randStdNormal * noiseScale);
      }
    }
  }
}
