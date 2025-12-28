using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// PCA-reduced embedding generated from oracle text embeddings.
/// Contains dimensionality-reduced vector representation of card text.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> OracleTextEmbedding → OraclePcaEmbedding
/// </para>
/// <para>
/// Each 384-dimensional oracle text embedding is reduced to a lower-dimensional
/// representation using Principal Component Analysis (PCA) via ML.NET.
/// This preserves the most important variance while reducing storage and
/// computational requirements.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Visualization of card embeddings in 2D or 3D space</item>
/// <item>Faster similarity computations with reduced dimensions</item>
/// <item>Feature extraction for downstream ML models</item>
/// <item>Exploratory data analysis and clustering</item>
/// </list>
/// </remarks>
public record OraclePcaEmbedding : IFlatSchema, IBinarySerializable, IStructuredSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
  /// Matches the ID from the original oracle embedding.
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
  /// PCA-reduced component vector.
  /// Vector represents the most important variance directions from the original embedding.
  /// Dimensionality is configurable (typically 2-50 components).
  /// </summary>
  [SerializedLabel("components")]
  public required float[] Components { get; init; }

  /// <summary>
  /// Dimensionality of the PCA component vector.
  /// Number of principal components retained from the original 384-dimensional embedding.
  /// </summary>
  [SerializedLabel("component_dim")]
  public required int ComponentDimension { get; init; }
}
