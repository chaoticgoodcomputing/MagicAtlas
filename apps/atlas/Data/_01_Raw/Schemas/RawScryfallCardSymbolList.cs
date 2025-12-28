using Flowthru.Abstractions;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Raw Scryfall card symbol DTO matching API exactly (snake_case, all strings/primitives).
/// </summary>
public record RawScryfallCardSymbol : IStructuredSerializable
{
  [SerializedLabel("object")]
  public string Object { get; init; } = "";

  [SerializedLabel("symbol")]
  public string Symbol { get; init; } = "";

  [SerializedLabel("loose_variant")]
  public string? Loose_Variant { get; init; }

  [SerializedLabel("english")]
  public string English { get; init; } = "";

  [SerializedLabel("svg_uri")]
  public string? Svg_Uri { get; init; }

  [SerializedLabel("transposable")]
  public bool Transposable { get; init; }

  [SerializedLabel("represents_mana")]
  public bool Represents_Mana { get; init; }

  [SerializedLabel("appears_in_mana_costs")]
  public bool Appears_In_Mana_Costs { get; init; }

  [SerializedLabel("mana_value")]
  public decimal? Mana_Value { get; init; }

  [SerializedLabel("hybrid")]
  public bool Hybrid { get; init; }

  [SerializedLabel("phyrexian")]
  public bool Phyrexian { get; init; }

  [SerializedLabel("cmc")]
  public decimal? Cmc { get; init; }

  [SerializedLabel("funny")]
  public bool Funny { get; init; }

  [SerializedLabel("colors")]
  public List<string>? Colors { get; init; }

  [SerializedLabel("gatherer_alternates")]
  public List<string>? Gatherer_Alternates { get; init; }
}

/// <summary>
/// Wrapper for Scryfall card symbol list response.
/// </summary>
public record RawScryfallCardSymbolList : IStructuredSerializable
{
  [SerializedLabel("object")]
  public string Object { get; init; } = "";

  [SerializedLabel("has_more")]
  public bool Has_More { get; init; }

  [SerializedLabel("data")]
  public List<RawScryfallCardSymbol> Data { get; init; } = new();
}
