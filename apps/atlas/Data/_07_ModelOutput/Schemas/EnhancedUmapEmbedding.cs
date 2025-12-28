using Flowthru.Abstractions;
using MagicAtlas.Data.Enums.Card;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// UMAP-reduced embedding enhanced with card metadata for visualization.
/// Combines dimensionality-reduced embeddings with card identity information.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Transformation:</strong> OracleUmapEmbedding + CardCoreData → EnhancedUmapEmbedding
/// </para>
/// <para>
/// Joins UMAP embeddings with card metadata to enable rich visualizations that
/// incorporate card properties such as color identity, mana value, and name.
/// </para>
/// <para>
/// <strong>Use Cases:</strong>
/// </para>
/// <list type="bullet">
/// <item>Creating color-coded scatter plots by card color identity</item>
/// <item>Sizing visualizations by mana value (CMC)</item>
/// <item>Interactive visualizations with card name tooltips</item>
/// <item>Analyzing semantic clustering patterns relative to card properties</item>
/// </list>
/// </remarks>
public record EnhancedUmapEmbedding
  : IFlatSchema,
    IBinarySerializable,
    IStructuredSerializable,
    ITextSerializable
{
  /// <summary>
  /// Unique identifier for this specific text entry.
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
  /// Original oracle text.
  /// </summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>
  /// UMAP-reduced embedding components.
  /// </summary>
  /// <remarks>
  /// Typically 2 or 3 dimensions for visualization.
  /// </remarks>
  [SerializedLabel("components")]
  public required float[] Components { get; init; }

  /// <summary>
  /// Number of dimensions in the UMAP embedding.
  /// </summary>
  [SerializedLabel("component_dimension")]
  public required int ComponentDimension { get; init; }

  /// <summary>
  /// The name of the card this text entry belongs to.
  /// </summary>
  /// <remarks>
  /// If this card has multiple faces, this field will contain both names separated by ␣//␣.
  /// </remarks>
  [SerializedLabel("card_name")]
  public required string CardName { get; init; }

  /// <summary>
  /// This card's color identity for Commander format deckbuilding.
  /// </summary>
  /// <remarks>
  /// Null or empty list indicates a colorless card.
  /// </remarks>
  [SerializedLabel("color_identity")]
  public List<ManaColor>? ColorIdentity { get; init; }

  /// <summary>
  /// The card's mana value (formerly converted mana cost).
  /// </summary>
  /// <remarks>
  /// Used for sizing visualizations proportional to casting cost.
  /// </remarks>
  [SerializedLabel("cmc")]
  public required decimal Cmc { get; init; }

  /// <summary>
  /// The card's price in USD.
  /// </summary>
  /// <remarks>
  /// Determined by first available: Usd, UsdFoil, or UsdEtched.
  /// Used for sizing visualizations proportional to market value.
  /// </remarks>
  [SerializedLabel("price")]
  public required decimal Price { get; init; }
}
