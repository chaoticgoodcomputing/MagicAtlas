using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Per-card metadata projected from <see cref="MagicAtlas.Data._03_Primary.Schemas.CardCoreData"/>
/// down to a flat, Arrow-friendly shape for the Plotly reporting step. One row per card; the
/// Python step joins to the per-fragment <see cref="ReportingPoint"/> rows on <see cref="CardId"/>.
/// </summary>
/// <remarks>
/// All fields are scalars (no nested types, no <c>decimal</c>, no enum lists) because pandas / Arrow
/// can't round-trip those cleanly through the subprocess boundary. <c>ColorIdentity</c> is
/// flattened to a WUBRG string (e.g. "WU" for azorius, "" for colorless) so Plotly can use it
/// directly as a category for color grouping. <c>TypeLine</c> reassembles the original Scryfall
/// "Legendary Creature — Elf Druid" form from the split <c>Types</c>/<c>Subtypes</c> fields.
/// </remarks>
[FlowthruSchema]
public partial record CardHoverInfo
{
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  [SerializedLabel("mana_cost")]
  public string? ManaCost { get; init; }

  [SerializedLabel("cmc")]
  public required double Cmc { get; init; }

  [SerializedLabel("type_line")]
  public required string TypeLine { get; init; }

  /// <summary>WUBRG-ordered color identity, e.g. "WU", "BR", "" for colorless.</summary>
  [SerializedLabel("color_identity")]
  public required string ColorIdentity { get; init; }

  [SerializedLabel("power")]
  public string? Power { get; init; }

  [SerializedLabel("toughness")]
  public string? Toughness { get; init; }

  [SerializedLabel("oracle_text")]
  public string? OracleText { get; init; }
}
