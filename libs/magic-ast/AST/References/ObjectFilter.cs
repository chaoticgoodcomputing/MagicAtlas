namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// Describes a filter for selecting objects.
/// e.g., "nontoken creature with flying you control"
/// </summary>
public sealed record ObjectFilter
{
  /// <summary>
  /// Card types to match: Creature, Artifact, Enchantment, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? CardTypes { get; init; }

  /// <summary>
  /// Subtypes to match: Human, Equipment, Aura, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Supertypes to match: Legendary, Basic, Snow, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Colors to match. An object passes the filter if it has at least one of
  /// the listed colors. Do NOT use this to encode colorlessness — colorless
  /// is the absence of color (Rule 105.1: "Colorless is not a color"). Use
  /// <see cref="IsColorless"/> instead.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Filters to colorless objects (those with no colors at all). Rule 105.1.
  /// Mutually exclusive with <see cref="Colors"/> in practice (a card cannot
  /// be both colorless and have a color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsColorless { get; init; }

  /// <summary>
  /// Filters to multicolored objects (those with two or more colors). Rule 105.5
  /// ("An object is multicolored if it has two or more colors"). Parallel axis to
  /// <see cref="IsColorless"/> — distinct from the "has any of these colors" semantics
  /// of <see cref="Colors"/>, which is satisfied by a single matching color and so
  /// cannot encode the "two or more" constraint.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsMulticolored { get; init; }

  /// <summary>
  /// Filters to monocolored objects (those with exactly one color). Rule 105.3
  /// ("An object is the color or colors of the mana symbols in its mana cost,
  /// regardless of the cost of that mana ... If an object has exactly one of
  /// these five colors, it is monocolored"). Parallel axis to
  /// <see cref="IsMulticolored"/> and <see cref="IsColorless"/> — the "exactly
  /// one color" constraint cannot be expressed by <see cref="Colors"/>, which
  /// encodes "has any of these colors" (satisfied by multicolored objects that
  /// happen to include a listed color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsMonocolored { get; init; }

  /// <summary>
  /// Who controls the objects: You, Opponent, Any.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Controller { get; init; }

  /// <summary>
  /// Additional characteristics: "nontoken", "with flying", "tapped", etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Characteristics { get; init; }

  /// <summary>
  /// Zone restriction: Battlefield, Graveyard, Hand, Library, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? Zone { get; init; }

  /// <summary>
  /// Power comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? PowerComparison { get; init; }

  /// <summary>
  /// Toughness comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? ToughnessComparison { get; init; }

  /// <summary>
  /// Mana value comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? ManaValueComparison { get; init; }

  /// <summary>
  /// Backward-looking lifecycle predicate restricting the filter.
  /// e.g., "a creature dealt damage by Zurgo this turn".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HistoryPredicate? History { get; init; }

  /// <summary>
  /// Location in source text. Only present for unparsed or partially-parsed nodes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public TextSpan? SourceSpan { get; init; }

  // Factory methods
  public static ObjectFilter Creature(TextSpan? span = null) =>
    new() { CardTypes = ["creature"], SourceSpan = span };

  public static ObjectFilter Permanent(TextSpan? span = null) =>
    new() { CardTypes = ["permanent"], SourceSpan = span };

  public static ObjectFilter Card(TextSpan? span = null) =>
    new() { CardTypes = ["card"], SourceSpan = span };

  public static ObjectFilter Player(TextSpan? span = null) =>
    new() { CardTypes = ["player"], SourceSpan = span };
}

/// <summary>
/// Filter for who controls an object.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControllerFilter
{
  You,
  Opponent,
  Any,
}

/// <summary>
/// A numeric comparison.
/// </summary>
public sealed record Comparison
{
  public required ComparisonOperator Operator { get; init; }

  public required int Value { get; init; }
}

/// <summary>
/// Comparison operators.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComparisonOperator
{
  LessThan,
  LessThanOrEqual,
  Equal,
  GreaterThanOrEqual,
  GreaterThan,
  NotEqual,
}

/// <summary>
/// Game zones where cards can exist.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Zone
{
  Battlefield,
  Graveyard,
  Hand,
  Library,
  Exile,
  Stack,
  CommandZone,
  Anywhere,
}
