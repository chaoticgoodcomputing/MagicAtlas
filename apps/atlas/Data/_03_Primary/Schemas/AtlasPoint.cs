using Flowthru.Core.Abstractions;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A 2D coordinate for a card in oracle-text embedding space.
/// Produced by the OracleEmbedding pipeline (BERT → UMAP, Python) and consumed by the atlas-api
/// to render the scatter view.
/// </summary>
public record AtlasPoint : IStructuredSerializable, IFlatSchema
{
  /// <summary>Scryfall card id.</summary>
  [SerializedLabel("id")]
  public required Guid Id { get; init; }

  /// <summary>UMAP component 1.</summary>
  [SerializedLabel("x")]
  public required double X { get; init; }

  /// <summary>UMAP component 2.</summary>
  [SerializedLabel("y")]
  public required double Y { get; init; }

  /// <summary>
  /// Ability-type classification of the embedded text (Keyword / Triggered / Activated / Passive).
  /// Currently always <c>"oracle"</c> — fragment classification is a future enhancement.
  /// </summary>
  [SerializedLabel("text_type")]
  public string TextType { get; init; } = "oracle";
}
