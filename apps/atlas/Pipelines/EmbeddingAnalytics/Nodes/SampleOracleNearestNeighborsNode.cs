using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Pipelines.EmbeddingAnalytics.Nodes;

/// <summary>
/// Samples oracle cards and finds their nearest neighbors in embedding space.
/// </summary>
public static class SampleOracleNearestNeighborsNode
{
  /// <summary>
  /// Configuration options for nearest neighbor sampling and analysis.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Number of nearest neighbors to find for each target card.
    /// </summary>
    public int NeighborCount { get; init; } = 10;

    /// <summary>
    /// Specific card names to analyze. If provided, these cards are used without random sampling.
    /// </summary>
    public string[]? TargetCardNames { get; init; } = null;

    /// <summary>
    /// Random sampling configuration. Used only if TargetCardNames is null or empty.
    /// </summary>
    public RandomSamplingOptions? RandomSampling { get; init; } = null;

    /// <summary>
    /// Oracle text types to include in similarity calculations.
    /// If null or empty, all ability types are included (excluding Full).
    /// </summary>
    /// <remarks>
    /// Example: To compare only triggered and activated abilities, set to
    /// [OracleTextType.TriggeredAbility, OracleTextType.ActivatedAbility].
    /// </remarks>
    public OracleTextType[]? IncludedTextTypes { get; init; } = null;

    /// <summary>
    /// Oracle text types to exclude from similarity calculations.
    /// Applied after IncludedTextTypes filter. Useful for excluding specific types
    /// like keyword abilities while keeping all others.
    /// </summary>
    /// <remarks>
    /// Example: To exclude keyword abilities, set to [OracleTextType.KeywordAbility].
    /// This is applied after IncludedTextTypes, so excluded types take precedence.
    /// </remarks>
    public OracleTextType[]? ExcludedTextTypes { get; init; } = null;
  }

  /// <summary>
  /// Configuration for random sampling of target cards.
  /// </summary>
  public record RandomSamplingOptions
  {
    /// <summary>
    /// Random seed for reproducible sampling. If null, sampling is non-deterministic.
    /// </summary>
    public int? Seed { get; init; } = null;

    /// <summary>
    /// Number of cards to randomly sample from the catalog.
    /// </summary>
    public int TargetCount { get; init; } = 5;
  }

  /// <summary>
  /// Creates a nearest neighbor analysis function.
  /// </summary>
  /// <param name="options">Configuration for target selection and neighbor count.</param>
  /// <returns>
  /// A function that analyzes target cards and finds their nearest neighbors in embedding space.
  /// </returns>
  public static Func<
    (
      IEnumerable<CardCoreData> coreData,
      IEnumerable<CardMetadata> metadata,
      IEnumerable<OracleTextEmbedding> embeddings
    ),
    Task<IEnumerable<NearestNeighborAnalysis>>
  > Create(Params options)
  {
    return async (input) =>
    {
      var (coreData, metadata, embeddings) = input;

      // Build lookup dictionaries for efficient access
      var coreDataDict = coreData.ToDictionary(c => c.Id, c => c);
      var metadataDict = metadata.ToDictionary(m => m.Id, m => m);

      // Filter embeddings to exclude full text and apply configured text type filters
      var abilityEmbeddings = FilterEmbeddingsByTextType(embeddings, options).ToList();

      // Group embeddings by card ID for efficient lookup
      var embeddingsByCard = abilityEmbeddings
        .GroupBy(e => e.CardId)
        .ToDictionary(g => g.Key, g => g.ToList());

      // Select target cards based on configuration
      var targetCards = SelectTargetCards(coreDataDict.Values, options);

      var results = new List<NearestNeighborAnalysis>();

      foreach (var targetCard in targetCards)
      {
        // Skip cards without embeddings
        if (!embeddingsByCard.ContainsKey(targetCard.Id))
        {
          continue;
        }

        var targetEmbeddings = embeddingsByCard[targetCard.Id];

        // Calculate similarities to all other cards
        var neighborSimilarities = new Dictionary<Guid, double>();

        foreach (var targetEmbed in targetEmbeddings)
        {
          foreach (var candidateEmbed in abilityEmbeddings)
          {
            // Skip embeddings from the same card
            if (candidateEmbed.CardId == targetCard.Id)
            {
              continue;
            }

            var similarity = CosineSimilarity(targetEmbed.Embedding, candidateEmbed.Embedding);

            // Keep maximum similarity for each neighbor card (deduplication)
            if (
              !neighborSimilarities.ContainsKey(candidateEmbed.CardId)
              || similarity > neighborSimilarities[candidateEmbed.CardId]
            )
            {
              neighborSimilarities[candidateEmbed.CardId] = similarity;
            }
          }
        }

        // Sort by similarity descending and take top N
        var topNeighbors = neighborSimilarities
          .OrderByDescending(kvp => kvp.Value)
          .Take(options.NeighborCount)
          .ToList();

        // Build neighbor match records
        var neighbors = topNeighbors
          .Select(kvp =>
          {
            var neighborCard = coreDataDict[kvp.Key];
            var neighborMeta = metadataDict[kvp.Key];

            return new NeighborMatch
            {
              Similarity = kvp.Value,
              NeighborCardName = neighborCard.Name,
              NeighborManaCost = neighborCard.ManaCost,
              NeighborOracleText = neighborCard
                .OracleText?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .ToList(),
              NeighborScryfallUri = neighborMeta.ScryfallUri,
              NeighborPrice =
                neighborCard.Prices?.Usd
                ?? neighborCard.Prices?.UsdFoil
                ?? neighborCard.Prices?.UsdEtched
                ?? 0.0m,
            };
          })
          .ToList();

        // Get target metadata
        var targetMeta = metadataDict[targetCard.Id];

        results.Add(
          new NearestNeighborAnalysis
          {
            TargetCardName = targetCard.Name,
            TargetManaCost = targetCard.ManaCost,
            TargetOracleText = targetCard
              .OracleText?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
              .ToList(),
            TargetScryfallUri = targetMeta.ScryfallUri,
            TargetPrice =
              targetCard.Prices?.Usd
              ?? targetCard.Prices?.UsdFoil
              ?? targetCard.Prices?.UsdEtched
              ?? 0.0m,
            Neighbors = neighbors,
          }
        );
      }

      return await Task.FromResult(results);
    };
  }

  /// <summary>
  /// Filters embeddings based on configured text type inclusion and exclusion rules.
  /// </summary>
  /// <param name="embeddings">All embeddings to filter.</param>
  /// <param name="options">Configuration specifying which text types to include/exclude.</param>
  /// <returns>Filtered embeddings matching the configured text type criteria.</returns>
  private static IEnumerable<OracleTextEmbedding> FilterEmbeddingsByTextType(
    IEnumerable<OracleTextEmbedding> embeddings,
    Params options
  )
  {
    var filtered = embeddings.Where(e => e.TextType != OracleTextType.Full);

    // Apply inclusion filter if specified
    if (options.IncludedTextTypes != null && options.IncludedTextTypes.Length > 0)
    {
      var includedSet = new HashSet<OracleTextType>(options.IncludedTextTypes);
      filtered = filtered.Where(e => includedSet.Contains(e.TextType));
    }

    // Apply exclusion filter (takes precedence over inclusion)
    if (options.ExcludedTextTypes != null && options.ExcludedTextTypes.Length > 0)
    {
      var excludedSet = new HashSet<OracleTextType>(options.ExcludedTextTypes);
      filtered = filtered.Where(e => !excludedSet.Contains(e.TextType));
    }

    return filtered;
  }

  /// <summary>
  /// Selects target cards based on configuration (specific names or random sampling).
  /// </summary>
  private static IEnumerable<CardCoreData> SelectTargetCards(
    IEnumerable<CardCoreData> coreData,
    Params options
  )
  {
    var coreDataList = coreData.ToList();

    // If specific card names are provided, use those
    if (options.TargetCardNames != null && options.TargetCardNames.Length > 0)
    {
      var nameSet = new HashSet<string>(options.TargetCardNames, StringComparer.OrdinalIgnoreCase);
      return coreDataList.Where(c => nameSet.Contains(c.Name));
    }

    // Otherwise, use random sampling
    if (options.RandomSampling == null)
    {
      throw new InvalidOperationException(
        "Either TargetCardNames or RandomSampling must be configured."
      );
    }

    var random = options.RandomSampling.Seed.HasValue
      ? new Random(options.RandomSampling.Seed.Value)
      : new Random();

    return coreDataList.OrderBy(_ => random.Next()).Take(options.RandomSampling.TargetCount);
  }

  /// <summary>
  /// Calculates cosine similarity between two embedding vectors.
  /// </summary>
  /// <param name="a">First embedding vector.</param>
  /// <param name="b">Second embedding vector.</param>
  /// <returns>Cosine similarity in range (0.0, 1.0].</returns>
  private static double CosineSimilarity(float[] a, float[] b)
  {
    if (a.Length != b.Length)
    {
      throw new ArgumentException("Embedding vectors must have the same dimension.");
    }

    double dotProduct = 0.0;
    double normA = 0.0;
    double normB = 0.0;

    for (int i = 0; i < a.Length; i++)
    {
      dotProduct += a[i] * b[i];
      normA += a[i] * a[i];
      normB += b[i] * b[i];
    }

    return dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB));
  }
}
