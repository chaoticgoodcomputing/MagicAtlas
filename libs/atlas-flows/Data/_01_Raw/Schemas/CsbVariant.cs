using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Narrow projection of Commander Spellbook's <c>variants.json</c> bulk dump
/// (<c>https://json.commanderspellbook.com/variants.json</c>, ~510 MB). We keep ONLY the fields
/// interaction triage needs — combo id, popularity (the ranking signal), color identity, the cards
/// used (name + Scryfall <c>oracleId</c>, the join key to our parse corpus), and the produced
/// results — and let System.Text.Json drop the rest (the ~10 image-URI fields per card, prices,
/// legalities, zone/card-state, prerequisites). Keeps the project decoupled from CSB's full schema.
/// </summary>
/// <remarks>CSB keys are camelCase; <c>[SerializedLabel]</c> aliases map them to PascalCase props on read.
///
/// <para>Promoted from tests/magic-ast-tests/Data/_01_Raw/Schemas/CsbVariant.cs (upstream-atlas-data-plan
/// §0/§6 P0) so the shippable library — not the test assembly — can regenerate combos.json.</para></remarks>
[FlowthruSchema]
public partial record CsbVariantsDump
{
  [SerializedLabel("timestamp")]
  public string Timestamp { get; init; } = "";

  [SerializedLabel("version")]
  public string Version { get; init; } = "";

  [SerializedLabel("variants")]
  public List<CsbVariant> Variants { get; init; } = [];
}

/// <summary>One combo (variant) — its id, popularity, identity, cards used, and produced results.</summary>
[FlowthruSchema]
public partial record CsbVariant
{
  [SerializedLabel("id")]
  public string Id { get; init; } = "";

  /// <summary>CSB's numeric popularity metric — the triage ranking signal.</summary>
  [SerializedLabel("popularity")]
  public int? Popularity { get; init; }

  [SerializedLabel("identity")]
  public string Identity { get; init; } = "";

  [SerializedLabel("uses")]
  public List<CsbUse> Uses { get; init; } = [];

  [SerializedLabel("produces")]
  public List<CsbProduces> Produces { get; init; } = [];
}

/// <summary>A card the combo uses (the bloat fields — quantity, zoneLocations, card-state — are dropped).</summary>
[FlowthruSchema]
public partial record CsbUse
{
  [SerializedLabel("card")]
  public CsbCard Card { get; init; } = new();
}

/// <summary>The card itself — name + the Scryfall <c>oracleId</c> we join on (image URIs dropped).</summary>
[FlowthruSchema]
public partial record CsbCard
{
  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("oracleId")]
  public string OracleId { get; init; } = "";
}

/// <summary>A result the combo produces (e.g. "Infinite mana") — prose, not machine-readable edges.</summary>
[FlowthruSchema]
public partial record CsbProduces
{
  [SerializedLabel("feature")]
  public CsbFeature Feature { get; init; } = new();
}

/// <summary>The produced feature's name.</summary>
[FlowthruSchema]
public partial record CsbFeature
{
  [SerializedLabel("name")]
  public string Name { get; init; } = "";
}
