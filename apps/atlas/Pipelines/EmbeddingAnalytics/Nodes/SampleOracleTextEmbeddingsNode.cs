using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

/// <summary>
/// Randomly samples oracle text embeddings for diagnostic analysis.
/// </summary>
/// <remarks>
/// <para>
/// This node provides a manageable subset of embeddings for visualization and analysis
/// without overwhelming output files or charts with the full dataset.
/// </para>
/// </remarks>
public static class SampleOracleTextEmbeddingsNode
{
  /// <summary>
  /// Configuration options for embedding sampling.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of embeddings to randomly sample from the dataset.
    /// </summary>
    public int SampleCount { get; init; } = 200;

    /// <summary>
    /// Random seed for reproducible sampling. If null, sampling is non-deterministic.
    /// </summary>
    public int? Seed { get; init; } = null;
  }

  /// <summary>
  /// Creates a sampling function that randomly selects N embeddings.
  /// </summary>
  /// <param name="options">Configuration for sample size and random seed.</param>
  /// <returns>
  /// A function that samples embeddings from the input dataset.
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<OracleTextEmbedding>>
  > Create(Params? options = null)
  {
    var config = options ?? new Params();

    return async (input) =>
    {
      var embeddings = input.ToList();

      // Use seed if provided for reproducibility
      var random = config.Seed.HasValue ? new Random(config.Seed.Value) : new Random();

      // Ensure we don't try to sample more than available
      var sampleSize = Math.Min(config.SampleCount, embeddings.Count);

      // Fisher-Yates shuffle for uniform random sampling
      var sampled = embeddings.OrderBy(_ => random.Next()).Take(sampleSize).ToList();

      return await Task.FromResult(sampled.AsEnumerable());
    };
  }
}
