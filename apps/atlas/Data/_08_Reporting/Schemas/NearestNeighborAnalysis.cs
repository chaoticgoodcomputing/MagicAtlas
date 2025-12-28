using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Diagnostic analysis of nearest neighbors in embedding space for a target card.
/// </summary>
/// <remarks>
/// <para>
/// This schema represents the result of nearest neighbor search in the embedding space,
/// showing which cards have the most semantically similar oracle text based on their
/// ability embeddings.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Validating embedding quality by inspecting similar cards</item>
/// <item>Discovering mechanically similar cards for deck building</item>
/// <item>Understanding card clustering in embedding space</item>
/// <item>Debugging embedding generation pipeline</item>
/// </list>
/// </remarks>
public record NearestNeighborAnalysis : IStructuredSerializable
{
  /// <summary>
  /// Name of the target card being analyzed.
  /// </summary>
  public required string TargetCardName { get; init; }

  /// <summary>
  /// Mana cost of the target card, if any.
  /// </summary>
  public string? TargetManaCost { get; init; }

  /// <summary>
  /// Full oracle text of the target card, split by line, if any.
  /// </summary>
  public List<string>? TargetOracleText { get; init; }

  /// <summary>
  /// Scryfall URI for the target card.
  /// </summary>
  public required string TargetScryfallUri { get; init; }

  /// <summary>
  /// Price of the target card in USD.
  /// </summary>
  public required decimal TargetPrice { get; init; }

  /// <summary>
  /// List of nearest neighbor cards in embedding space, ordered by similarity descending.
  /// </summary>
  /// <remarks>
  /// Neighbors are deduplicated by card ID, keeping the maximum similarity score across
  /// all embedding types (keyword, activated, triggered, passive abilities).
  /// </remarks>
  public List<NeighborMatch> Neighbors { get; init; } = new();
}

/// <summary>
/// A single nearest neighbor match in embedding space.
/// </summary>
public record NeighborMatch
{
  /// <summary>
  /// Cosine similarity score between target and neighbor embeddings.
  /// Range: (0.0, 1.0], where 1.0 is identical and values closer to 1.0 are more similar.
  /// </summary>
  /// <remarks>
  /// If a neighbor card has multiple embedding types, this represents the maximum
  /// similarity across all types.
  /// </remarks>
  public required double Similarity { get; init; }

  /// <summary>
  /// Name of the neighboring card.
  /// </summary>
  public required string NeighborCardName { get; init; }

  /// <summary>
  /// Mana cost of the neighboring card, if any.
  /// </summary>
  public string? NeighborManaCost { get; init; }

  /// <summary>
  /// Full oracle text of the neighboring card, split by line, if any.
  /// </summary>
  public List<string>? NeighborOracleText { get; init; }

  /// <summary>
  /// Scryfall URI for the neighboring card.
  /// </summary>
  public required string NeighborScryfallUri { get; init; }

  /// <summary>
  /// Price of the neighboring card in USD.
  /// </summary>
  public required decimal NeighborPrice { get; init; }
}
