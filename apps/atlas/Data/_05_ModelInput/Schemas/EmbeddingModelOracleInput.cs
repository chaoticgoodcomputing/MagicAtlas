using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._05_ModelInput.Schemas;

/// <summary>
/// Flattened oracle text entry suitable for embedding model input.
/// Each card with oracle text produces multiple entries (one full text + one per ability).
/// </summary>
public record EmbeddingModelOracleInput : IFlatSchema, IBinarySerializable, ITextSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// Used to track individual abilities through the embedding pipeline.
  /// </summary>
  [SerializedLabel("text_entry_id")]
  public required Guid TextEntryId { get; init; }

  /// <summary>
  /// Scryfall card ID.
  /// </summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>
  /// Type of oracle text entry.
  /// </summary>
  [SerializedLabel("text_type")]
  public required OracleTextType TextType { get; init; }

  /// <summary>
  /// Raw text content for this entry.
  /// </summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }
}
