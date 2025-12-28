using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data.Enums.Card;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Pipelines.UmapExploration.Nodes;

/// <summary>
/// Filters oracle text embeddings by text type, then samples a specified percentage.
/// </summary>
/// <remarks>
/// <para>
/// Two-stage process:
/// </para>
/// <list type="number">
/// <item><strong>Filtering:</strong> Removes specified text types (e.g., Full text)</item>
/// <item><strong>Sampling:</strong> Randomly selects a percentage of remaining embeddings</item>
/// </list>
/// <para>
/// <strong>Use Case:</strong> Reduce dataset size for faster UMAP hyperparameter exploration
/// while maintaining representative distribution across text types.
/// </para>
/// <para>
/// <strong>Example:</strong> If filtering out Full text leaves 10,000 embeddings,
/// and sample percentage is 0.5, the output will contain 5,000 embeddings.
/// </para>
/// </remarks>
public static class FilterAndSampleEmbeddingsNode
{
  /// <summary>
  /// Configuration options for filtering and sampling.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Text types to exclude from the dataset before sampling.
    /// </summary>
    /// <remarks>
    /// Default: excludes Full text to focus on individual ability types.
    /// </remarks>
    public HashSet<OracleTextType> ExcludedTextTypes { get; init; } = new() { OracleTextType.Full };

    /// <summary>
    /// Percentage of filtered embeddings to sample.
    /// </summary>
    /// <remarks>
    /// <para>Range: 0.0-1.0</para>
    /// <para>Example: 0.5 = sample 50% of filtered embeddings</para>
    /// <para>Default: 0.5 (50%)</para>
    /// </remarks>
    public float SamplePercentage { get; init; } = 0.5f;

    /// <summary>
    /// Random seed for reproducible sampling.
    /// </summary>
    /// <remarks>
    /// Set to a fixed value to ensure identical samples across runs.
    /// </remarks>
    public int RandomSeed { get; init; } = 42;
  }

  /// <summary>
  /// Creates a filter and sample transformation function.
  /// </summary>
  /// <param name="options">Configuration for filtering and sampling.</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  /// <returns>
  /// Transform function that filters and samples embeddings.
  /// </returns>
  public static Func<
    IEnumerable<OracleTextEmbedding>,
    Task<IEnumerable<OracleTextEmbedding>>
  > Create(Params? options = null, ILogger? logger = null)
  {
    var opts = options ?? new Params();

    if (opts.SamplePercentage <= 0 || opts.SamplePercentage > 1.0f)
    {
      throw new ArgumentException(
        $"SamplePercentage must be in range (0, 1]. Got: {opts.SamplePercentage}",
        nameof(options)
      );
    }

    return async (embeddings) =>
    {
      var embeddingsList = embeddings.ToList();
      logger?.LogInformation("Starting with {Count} total embeddings", embeddingsList.Count);

      // Stage 1: Filter by text type
      var filtered = embeddingsList
        .Where(e => !opts.ExcludedTextTypes.Contains(e.TextType))
        .ToList();

      logger?.LogInformation(
        "After filtering: {FilteredCount} embeddings (excluded {ExcludedCount} from types: {Types})",
        filtered.Count,
        embeddingsList.Count - filtered.Count,
        string.Join(", ", opts.ExcludedTextTypes)
      );

      if (filtered.Count == 0)
      {
        logger?.LogWarning(
          "All embeddings were filtered out. Check ExcludedTextTypes configuration."
        );
        return Enumerable.Empty<OracleTextEmbedding>();
      }

      // Stage 2: Random sampling
      var random = new Random(opts.RandomSeed);
      var targetCount = (int)Math.Ceiling(filtered.Count * opts.SamplePercentage);

      // Fisher-Yates shuffle for random sampling
      var sampled = filtered.OrderBy(_ => random.Next()).Take(targetCount).ToList();

      logger?.LogInformation(
        "Sampled {SampleCount} embeddings ({Percentage:P1} of {FilteredCount})",
        sampled.Count,
        opts.SamplePercentage,
        filtered.Count
      );

      return await Task.FromResult(sampled.AsEnumerable());
    };
  }
}
