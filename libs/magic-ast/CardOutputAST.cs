namespace MagicAST;

using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.AST.Costs;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// The complete output AST for a parsed card.
/// Contains only universally-present fields; type-specific data lives in Attributes.
/// </summary>
public sealed record CardOutputAST
{
  /// <summary>
  /// The card's name. All cards have a name.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// The parsed type line. All cards have types.
  /// </summary>
  public required TypeLineAST TypeLine { get; init; }

  /// <summary>
  /// The parsed oracle text containing all abilities.
  /// All cards have oracle text (even if empty).
  /// </summary>
  public required CardOracle Oracle { get; init; }

  /// <summary>
  /// Type-specific attributes (mana cost, stats, loyalty, etc.).
  /// Only present when applicable to this card's type.
  /// </summary>
  public required IReadOnlyList<CardAttribute> Attributes { get; init; }

  /// <summary>
  /// For multi-faced cards, each face parsed separately.
  /// Null for single-faced cards.
  /// </summary>
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
  public required string Name { get; init; }

  /// <summary>
  /// The face's parsed type line.
  /// </summary>
  public required TypeLineAST TypeLine { get; init; }

  /// <summary>
  /// The face's parsed oracle text.
  /// </summary>
  public required CardOracle Oracle { get; init; }

  /// <summary>
  /// Type-specific attributes for this face.
  /// </summary>
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
  public required string Raw { get; init; }

  /// <summary>
  /// Supertypes (Legendary, Basic, Snow, World).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Card types (Creature, Artifact, Enchantment, etc.).
  /// </summary>
  public required IReadOnlyList<string> Types { get; init; }

  /// <summary>
  /// Subtypes (creature types, artifact types, etc.).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }
}

/// <summary>
/// A polymorphic card attribute. Different card types have different attributes.
/// </summary>
[PolymorphicBase("Kind")]
[JsonConverter(typeof(PolymorphicReflectionConverter<CardAttribute>))]
public abstract record CardAttribute;

/// <summary>
/// Mana cost attribute (most non-land cards).
/// </summary>
[CardAttributeKind("manaCost")]
public sealed record ManaCostAttribute : CardAttribute
{
  public required string Raw { get; init; }

  public required IReadOnlyList<ManaSymbol> Symbols { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? ManaValue { get; init; }

  public bool IsVariable { get; init; }
}

/// <summary>
/// Colors derived from mana cost or color indicator.
/// </summary>
[CardAttributeKind("colors")]
public sealed record ColorsAttribute : CardAttribute
{
  public required IReadOnlyList<string> Colors { get; init; }
}

/// <summary>
/// Color identity (for Commander format).
/// </summary>
[CardAttributeKind("colorIdentity")]
public sealed record ColorIdentityAttribute : CardAttribute
{
  public required IReadOnlyList<string> ColorIdentity { get; init; }
}

/// <summary>
/// Creature power and toughness.
/// </summary>
[CardAttributeKind("creatureStats")]
public sealed record CreatureStatsAttribute : CardAttribute
{
  public required PowerToughnessValue Power { get; init; }

  public required PowerToughnessValue Toughness { get; init; }
}

/// <summary>
/// A power or toughness value that may be fixed, variable, or derived.
/// </summary>
[PolymorphicBase("ValueType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<PowerToughnessValue>))]
public abstract record PowerToughnessValue
{
  public required string Raw { get; init; }
}

/// <summary>
/// A fixed numeric power/toughness value.
/// </summary>
[PowerToughnessKind("fixed")]
public sealed record FixedPTValue : PowerToughnessValue
{
  public required int Value { get; init; }
}

/// <summary>
/// A variable power/toughness (just "*").
/// </summary>
[PowerToughnessKind("variable")]
public sealed record VariablePTValue : PowerToughnessValue
{
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DerivedFrom { get; init; }
}

/// <summary>
/// A derived power/toughness like "1+*" or "*+1".
/// </summary>
[PowerToughnessKind("derived")]
public sealed record DerivedPTValue : PowerToughnessValue
{
  public int BaseValue { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DerivedFrom { get; init; }
}

/// <summary>
/// Planeswalker starting loyalty.
/// </summary>
[CardAttributeKind("loyalty")]
public sealed record LoyaltyAttribute : CardAttribute
{
  public required string Raw { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? StartingLoyalty { get; init; }

  public bool IsVariable { get; init; }
}

/// <summary>
/// Battle defense value.
/// </summary>
[CardAttributeKind("defense")]
public sealed record DefenseAttribute : CardAttribute
{
  public required int Defense { get; init; }
}

/// <summary>
/// Additional costs parsed from oracle text.
/// </summary>
[CardAttributeKind("additionalCosts")]
public sealed record AdditionalCostsAttribute : CardAttribute
{
  public required IReadOnlyList<AdditionalCost> Costs { get; init; }
}

/// <summary>
/// Alternative costs parsed from oracle text.
/// </summary>
[CardAttributeKind("alternativeCosts")]
public sealed record AlternativeCostsAttribute : CardAttribute
{
  public required IReadOnlyList<AlternativeCost> Costs { get; init; }
}

/// <summary>
/// Cost reductions parsed from oracle text.
/// </summary>
[CardAttributeKind("costReductions")]
public sealed record CostReductionsAttribute : CardAttribute
{
  public required IReadOnlyList<CostReduction> Reductions { get; init; }
}

/// <summary>
/// Card layout for multi-faced cards.
/// </summary>
[CardAttributeKind("layout")]
public sealed record LayoutAttribute : CardAttribute
{
  public required string Layout { get; init; }
}
