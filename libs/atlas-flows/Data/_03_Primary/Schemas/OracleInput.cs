using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// A single oracle-text fragment fed to the Python embedding step — one row per ability,
/// not per card. A card with "Flying / When this enters, draw a card. / {T}: add {G}." yields
/// three rows with text types "keyword", "triggered", and "activated" respectively.
/// </summary>
/// <remarks>
/// Deliberately Arrow-friendly (no <c>decimal</c>, no nested types) because the row ships to a
/// Python subprocess via Apache Arrow.
/// </remarks>
[FlowthruSchema]
public partial record OracleInput
{
  /// <summary>
  /// Globally-unique identifier for this fragment, assigned at projection time. The join key for
  /// every downstream artifact (BERT vectors, 2D atlas points, cluster assignments, cluster
  /// labels) — replaces the prior reliance on row alignment between flows. Regenerated each
  /// pipeline run; not stable across runs.
  /// </summary>
  [SerializedLabel("point_id")]
  public required Guid PointId { get; init; }

  /// <summary>The Scryfall card id. NOT unique across rows — multiple fragments per card.</summary>
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  /// <summary>The cleaned ability text (reminder-parentheticals stripped).</summary>
  [SerializedLabel("text")]
  public required string Text { get; init; }

  /// <summary>One of: keyword, named_triggered, triggered, activated, passive.</summary>
  [SerializedLabel("text_type")]
  public required string TextType { get; init; }
}
