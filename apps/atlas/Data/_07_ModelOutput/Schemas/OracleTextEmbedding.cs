using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Oracle text embedding generated from sentence-transformers model.
/// Contains semantic vector representation of card text.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> EmbeddingModelOracleInput → OracleTextEmbedding
/// </para>
/// <para>
/// Each oracle text entry (full text or individual ability) is transformed
/// into a 384-dimensional vector using the all-MiniLM-L6-v2 model.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Semantic similarity search between cards</item>
/// <item>Clustering cards by mechanical similarity</item>
/// <item>Training downstream ML models on card mechanics</item>
/// <item>Recommendation systems for deck building</item>
/// </list>
/// </remarks>
public record OracleTextEmbedding : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// Matches the ID from the original oracle input.
  /// </summary>
  [SerializedLabel("text_entry_id")]
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry (full text, keyword ability, etc.).
  /// </summary>
  [SerializedLabel("text_type")]
  public required OracleTextType TextType { get; init; }

  /// <summary>
  /// Text content that was embedded (full oracle text or individual ability).
  /// </summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>
  /// Sentence embedding vector (384 dimensions).
  /// Vector represents semantic meaning of the oracle text in high-dimensional space.
  /// Similar mechanics produce vectors with high cosine similarity.
  /// </summary>
  [SerializedLabel("embedding")]
  public required float[] Embedding { get; init; }

  /// <summary>
  /// Dimensionality of the embedding vector.
  /// Should always be 384 for all-MiniLM-L6-v2 model.
  /// </summary>
  [SerializedLabel("embedding_dim")]
  public required int EmbeddingDimension { get; init; }
}
