using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._04_Feature.Schemas;

/// <summary>
/// Tokenized BERT input tensors for oracle text entries.
/// Intermediate representation between raw text and ONNX model inference.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> EmbeddingModelOracleInput → TokenizedBertInput
/// </para>
/// <para>
/// Each oracle text string is tokenized into three tensor arrays required by BERT models:
/// </para>
/// <list type="bullet">
/// <item><strong>input_ids:</strong> Token vocabulary indices</item>
/// <item><strong>attention_mask:</strong> Valid token positions (1) vs padding (0)</item>
/// <item><strong>token_type_ids:</strong> Segment IDs (typically all 0s for single sentences)</item>
/// </list>
/// <para>
/// <strong>Tokenizer:</strong> BertUncasedBaseTokenizer (matches all-MiniLM-L6-v2 base)
/// </para>
/// <para>
/// <strong>Max Length:</strong> 512 tokens (entries exceeding this are dropped)
/// </para>
/// <para>
/// <strong>Storage:</strong> In-memory only (not persisted to disk)
/// </para>
/// </remarks>
public record TokenizedBertInput : IFlatSchema, IBinarySerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// Flows through the pipeline to enable proper joining of embeddings with original text.
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
  /// Token vocabulary indices (input_ids tensor).
  /// Shape: [sequence_length]. Vocabulary size: 30,522 (BERT-base-uncased).
  /// </summary>
  [SerializedLabel("input_ids")]
  public required long[] InputIds { get; init; }

  /// <summary>
  /// Attention mask indicating valid tokens (1) vs padding (0).
  /// Shape: [sequence_length]. Values: 1 for real tokens, 0 for padding.
  /// </summary>
  [SerializedLabel("attention_mask")]
  public required long[] AttentionMask { get; init; }

  /// <summary>
  /// Token type IDs for segment distinction.
  /// Shape: [sequence_length]. Values: Typically all 0s for single-sentence inputs.
  /// </summary>
  [SerializedLabel("token_type_ids")]
  public required long[] TokenTypeIds { get; init; }
}
