namespace MagicAST;

using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.AST.Costs;

/// <summary>
/// The complete output AST for a parsed card.
/// Contains only universally-present fields; type-specific data lives in Attributes.
/// </summary>
public sealed record CardOutputAST
{
  /// <summary>
  /// The card's name. All cards have a name.
  /// </summary>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  /// <summary>
  /// The parsed type line. All cards have types.
  /// </summary>
  [JsonPropertyName("typeLine")]
  public required TypeLineAST TypeLine { get; init; }

  /// <summary>
  /// The parsed oracle text containing all abilities.
  /// All cards have oracle text (even if empty).
  /// </summary>
  [JsonPropertyName("oracle")]
  public required CardOracle Oracle { get; init; }

  /// <summary>
  /// Type-specific attributes (mana cost, stats, loyalty, etc.).
  /// Only present when applicable to this card's type.
  /// </summary>
  [JsonPropertyName("attributes")]
  public required IReadOnlyList<CardAttribute> Attributes { get; init; }

  /// <summary>
  /// For multi-faced cards, each face parsed separately.
  /// Null for single-faced cards.
  /// </summary>
  [JsonPropertyName("faces")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<CardFaceAST>? Faces { get; init; }
}

/// <summary>
/// A parsed face of a multi-faced card.
/// Uses the same attribute pattern as the root card.
/// </summary>
public sealed record CardFaceAST
{
  /// <summary>
  /// The face's name.
  /// </summary>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  /// <summary>
  /// The face's parsed type line.
  /// </summary>
  [JsonPropertyName("typeLine")]
  public required TypeLineAST TypeLine { get; init; }

  /// <summary>
  /// The face's parsed oracle text.
  /// </summary>
  [JsonPropertyName("oracle")]
  public required CardOracle Oracle { get; init; }

  /// <summary>
  /// Type-specific attributes for this face.
  /// </summary>
  [JsonPropertyName("attributes")]
  public required IReadOnlyList<CardAttribute> Attributes { get; init; }
}

/// <summary>
/// Parsed type line structure. All cards have types.
/// </summary>
public sealed record TypeLineAST
{
  /// <summary>
  /// The raw type line string.
  /// </summary>
  [JsonPropertyName("raw")]
  public required string Raw { get; init; }

  /// <summary>
  /// Supertypes (Legendary, Basic, Snow, World).
  /// </summary>
  [JsonPropertyName("supertypes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Card types (Creature, Artifact, Enchantment, etc.).
  /// </summary>
  [JsonPropertyName("types")]
  public required IReadOnlyList<string> Types { get; init; }

  /// <summary>
  /// Subtypes (creature types, artifact types, etc.).
  /// </summary>
  [JsonPropertyName("subtypes")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }
}

/// <summary>
/// A polymorphic card attribute. Different card types have different attributes.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(ManaCostAttribute), "manaCost")]
[JsonDerivedType(typeof(ColorsAttribute), "colors")]
[JsonDerivedType(typeof(ColorIdentityAttribute), "colorIdentity")]
[JsonDerivedType(typeof(CreatureStatsAttribute), "creatureStats")]
[JsonDerivedType(typeof(LoyaltyAttribute), "loyalty")]
[JsonDerivedType(typeof(DefenseAttribute), "defense")]
[JsonDerivedType(typeof(AdditionalCostsAttribute), "additionalCosts")]
[JsonDerivedType(typeof(AlternativeCostsAttribute), "alternativeCosts")]
[JsonDerivedType(typeof(CostReductionsAttribute), "costReductions")]
[JsonDerivedType(typeof(LayoutAttribute), "layout")]
public abstract record CardAttribute;

/// <summary>
/// Mana cost attribute (most non-land cards).
/// </summary>
public sealed record ManaCostAttribute : CardAttribute
{
  [JsonPropertyName("raw")]
  public required string Raw { get; init; }

  [JsonPropertyName("symbols")]
  public required IReadOnlyList<ManaSymbol> Symbols { get; init; }

  [JsonPropertyName("manaValue")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? ManaValue { get; init; }

  [JsonPropertyName("isVariable")]
  public bool IsVariable { get; init; }
}

/// <summary>
/// Colors derived from mana cost or color indicator.
/// </summary>
public sealed record ColorsAttribute : CardAttribute
{
  [JsonPropertyName("colors")]
  public required IReadOnlyList<string> Colors { get; init; }
}

/// <summary>
/// Color identity (for Commander format).
/// </summary>
public sealed record ColorIdentityAttribute : CardAttribute
{
  [JsonPropertyName("colorIdentity")]
  public required IReadOnlyList<string> ColorIdentity { get; init; }
}

/// <summary>
/// Creature power and toughness.
/// </summary>
public sealed record CreatureStatsAttribute : CardAttribute
{
  [JsonPropertyName("power")]
  public required PowerToughnessValue Power { get; init; }

  [JsonPropertyName("toughness")]
  public required PowerToughnessValue Toughness { get; init; }
}

/// <summary>
/// A power or toughness value that may be fixed, variable, or derived.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "valueType")]
[JsonDerivedType(typeof(FixedPTValue), "fixed")]
[JsonDerivedType(typeof(VariablePTValue), "variable")]
[JsonDerivedType(typeof(DerivedPTValue), "derived")]
public abstract record PowerToughnessValue
{
  [JsonPropertyName("raw")]
  public required string Raw { get; init; }
}

/// <summary>
/// A fixed numeric power/toughness value.
/// </summary>
public sealed record FixedPTValue : PowerToughnessValue
{
  [JsonPropertyName("value")]
  public required int Value { get; init; }
}

/// <summary>
/// A variable power/toughness (just "*").
/// </summary>
public sealed record VariablePTValue : PowerToughnessValue
{
  [JsonPropertyName("derivedFrom")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DerivedFrom { get; init; }
}

/// <summary>
/// A derived power/toughness like "1+*" or "*+1".
/// </summary>
public sealed record DerivedPTValue : PowerToughnessValue
{
  [JsonPropertyName("baseValue")]
  public int BaseValue { get; init; }

  [JsonPropertyName("derivedFrom")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DerivedFrom { get; init; }
}

/// <summary>
/// Planeswalker starting loyalty.
/// </summary>
public sealed record LoyaltyAttribute : CardAttribute
{
  [JsonPropertyName("raw")]
  public required string Raw { get; init; }

  [JsonPropertyName("startingLoyalty")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? StartingLoyalty { get; init; }

  [JsonPropertyName("isVariable")]
  public bool IsVariable { get; init; }
}

/// <summary>
/// Battle defense value.
/// </summary>
public sealed record DefenseAttribute : CardAttribute
{
  [JsonPropertyName("defense")]
  public required int Defense { get; init; }
}

/// <summary>
/// Additional costs parsed from oracle text.
/// </summary>
public sealed record AdditionalCostsAttribute : CardAttribute
{
  [JsonPropertyName("costs")]
  public required IReadOnlyList<AdditionalCost> Costs { get; init; }
}

/// <summary>
/// Alternative costs parsed from oracle text.
/// </summary>
public sealed record AlternativeCostsAttribute : CardAttribute
{
  [JsonPropertyName("costs")]
  public required IReadOnlyList<AlternativeCost> Costs { get; init; }
}

/// <summary>
/// Cost reductions parsed from oracle text.
/// </summary>
public sealed record CostReductionsAttribute : CardAttribute
{
  [JsonPropertyName("reductions")]
  public required IReadOnlyList<CostReduction> Reductions { get; init; }
}

/// <summary>
/// Card layout for multi-faced cards.
/// </summary>
public sealed record LayoutAttribute : CardAttribute
{
  [JsonPropertyName("layout")]
  public required string Layout { get; init; }
}
