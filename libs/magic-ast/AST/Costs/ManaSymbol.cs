namespace MagicAST.AST.Costs;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a single mana symbol like {W}, {2}, {G/U}, {2/W}, {G/P}.
/// </summary>
public sealed record ManaSymbol
{
  /// <summary>
  /// The kind of mana symbol.
  /// </summary>
  [JsonPropertyName("kind")]
  public required ManaSymbolKind Kind { get; init; }

  /// <summary>
  /// For colored mana, the color(s).
  /// </summary>
  [JsonPropertyName("colors")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ManaColor>? Colors { get; init; }

  /// <summary>
  /// For generic mana, the amount. For hybrid mana with generic, the generic portion.
  /// </summary>
  [JsonPropertyName("genericAmount")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? GenericAmount { get; init; }

  /// <summary>
  /// True if this is a Phyrexian mana symbol (can be paid with 2 life).
  /// </summary>
  [JsonPropertyName("isPhyrexian")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool IsPhyrexian { get; init; }

  /// <summary>
  /// True if this is a snow mana symbol {S}.
  /// </summary>
  [JsonPropertyName("isSnow")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool IsSnow { get; init; }

  // Factory methods for common symbols
  public static ManaSymbol White =>
    new() { Kind = ManaSymbolKind.Colored, Colors = [ManaColor.White] };
  public static ManaSymbol Blue =>
    new() { Kind = ManaSymbolKind.Colored, Colors = [ManaColor.Blue] };
  public static ManaSymbol Black =>
    new() { Kind = ManaSymbolKind.Colored, Colors = [ManaColor.Black] };
  public static ManaSymbol Red => new() { Kind = ManaSymbolKind.Colored, Colors = [ManaColor.Red] };
  public static ManaSymbol Green =>
    new() { Kind = ManaSymbolKind.Colored, Colors = [ManaColor.Green] };
  public static ManaSymbol Colorless => new() { Kind = ManaSymbolKind.Colorless };

  public static ManaSymbol Generic(int amount) =>
    new() { Kind = ManaSymbolKind.Generic, GenericAmount = amount };

  public static ManaSymbol Variable => new() { Kind = ManaSymbolKind.Variable };

  public static ManaSymbol Hybrid(ManaColor a, ManaColor b) =>
    new() { Kind = ManaSymbolKind.Hybrid, Colors = [a, b] };

  public static ManaSymbol HybridGeneric(int amount, ManaColor color) =>
    new()
    {
      Kind = ManaSymbolKind.HybridGeneric,
      Colors = [color],
      GenericAmount = amount,
    };

  public static ManaSymbol Phyrexian(ManaColor color) =>
    new()
    {
      Kind = ManaSymbolKind.Colored,
      Colors = [color],
      IsPhyrexian = true,
    };
}

/// <summary>
/// The kind of mana symbol.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ManaSymbolKind>))]
public enum ManaSymbolKind
{
  /// <summary>A single colored mana: {W}, {U}, {B}, {R}, {G}</summary>
  [JsonStringEnumMemberName("colored")]
  Colored,

  /// <summary>Colorless mana: {C}</summary>
  [JsonStringEnumMemberName("colorless")]
  Colorless,

  /// <summary>Generic mana: {1}, {2}, {3}, etc.</summary>
  [JsonStringEnumMemberName("generic")]
  Generic,

  /// <summary>Variable mana: {X}, {Y}, {Z}</summary>
  [JsonStringEnumMemberName("variable")]
  Variable,

  /// <summary>Hybrid mana: {W/U}, {B/G}, etc.</summary>
  [JsonStringEnumMemberName("hybrid")]
  Hybrid,

  /// <summary>Hybrid with generic: {2/W}, {2/U}, etc.</summary>
  [JsonStringEnumMemberName("hybridGeneric")]
  HybridGeneric,

  /// <summary>Snow mana: {S}</summary>
  [JsonStringEnumMemberName("snow")]
  Snow,
}

/// <summary>
/// The five colors of Magic.
/// Uses single-letter codes (W/U/B/R/G) for JSON serialization.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ManaColor
{
  [JsonStringEnumMemberName("W")]
  White,

  [JsonStringEnumMemberName("U")]
  Blue,

  [JsonStringEnumMemberName("B")]
  Black,

  [JsonStringEnumMemberName("R")]
  Red,

  [JsonStringEnumMemberName("G")]
  Green,
}
