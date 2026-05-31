namespace MagicAST.AST.Quantities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Base type for numeric quantities that may be literal, variable, or derived.
/// </summary>
[PolymorphicBase("QuantityType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Quantity>))]
public abstract record Quantity;

/// <summary>
/// A literal numeric value like 1, 2, 3.
/// </summary>
[OracleQuantity("literal")]
public sealed record LiteralQuantity : Quantity
{
  /// <summary>
  /// The literal value.
  /// </summary>
  public required int Value { get; init; }

  /// <summary>
  /// Creates a literal quantity.
  /// </summary>
  public static LiteralQuantity Of(int value) => new() { Value = value };
}

/// <summary>
/// A variable quantity like X, Y, Z.
/// </summary>
[OracleQuantity("variable")]
public sealed record VariableQuantity : Quantity
{
  /// <summary>
  /// The variable name (X, Y, Z).
  /// </summary>
  public required string Name { get; init; }

  public static VariableQuantity X => new() { Name = "X" };
  public static VariableQuantity Y => new() { Name = "Y" };
  public static VariableQuantity Z => new() { Name = "Z" };
}

/// <summary>
/// A quantity derived from a characteristic of an object.
/// e.g., "equal to its power", "equal to the number of cards in your hand"
/// </summary>
[OracleQuantity("derived")]
public sealed record DerivedQuantity : Quantity
{
  /// <summary>
  /// What characteristic the value is derived from.
  /// </summary>
  public required DerivedKind DerivedFrom { get; init; }

  /// <summary>
  /// The source object for the derivation.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Source { get; init; }
}

/// <summary>
/// What a derived quantity is based on.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DerivedKind
{
  Power,
  Toughness,
  ManaValue,
  LifeTotal,
  CardsInHand,
  CardsInGraveyard,
  DamageDealt,
  LifeGained,
  LifeLost,
  Other,
}

/// <summary>
/// A quantity that counts objects matching a filter.
/// e.g., "the number of creatures you control".
///
/// <para>
/// <see cref="CountOf"/> is a structured <see cref="ObjectFilter"/> rather than
/// a verbatim phrase: counting "lands you control" or "Aura and Equipment
/// attached to it" is the same object-selection semantics the rest of the AST
/// expresses through <see cref="ObjectFilter"/> (card types, subtypes,
/// supertypes, controller, the <see cref="ObjectFilter.AttachedTo"/> relational
/// axis). Counting <i>counters</i> — a number that is not a count of objects —
/// is a distinct shape carried by <see cref="CounterCountQuantity"/>.
/// </para>
/// </summary>
[OracleQuantity("count")]
public sealed record CountQuantity : Quantity
{
  /// <summary>
  /// The class of objects being counted.
  /// </summary>
  public required ObjectFilter CountOf { get; init; }
}

/// <summary>
/// A quantity that counts <b>counters</b> of a named kind on an object —
/// "for each oil counter on it" (Serum-Core Chimera). Distinct from
/// <see cref="CountQuantity"/>: a counter is not an object, so an
/// <see cref="ObjectFilter"/> cannot express "how many oil counters are on this
/// permanent". The kind of counter (<see cref="CounterType"/>) and the object
/// bearing them (<see cref="On"/>) are the two axes.
/// </summary>
[OracleQuantity("counterCount")]
public sealed record CounterCountQuantity : Quantity
{
  /// <summary>
  /// The counter kind being counted, e.g. "oil", "+1/+1", "charge".
  /// </summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// The object whose counters are counted — "on it" is <c>{Kind:"Self"}</c>.
  /// </summary>
  public required ObjectReference On { get; init; }
}

/// <summary>
/// A quantity equal to the number of times a spell was "kicked" — the count of times the
/// additional cost granted by a kicker-family keyword was paid as the spell was cast.
/// "Create a 2/2 green Wolf creature token for each time it was kicked" (Wolfbriar
/// Elemental). CR 702.33d defines the kicked state; CR 702.33c folds a multikicker cost
/// into a kicker cost, so the count is keyed on <see cref="KeywordAbility.Kicker"/>.
///
/// <para>
/// Reference-not-resolution (ADR 0004), the quantity sibling of
/// <see cref="MagicAST.AST.Abilities.KickedCondition"/>: keyed on the producing keyword's
/// typed <see cref="KeywordAbility"/> identity (the linked ability of CR 702.33e), NOT a
/// variable threaded from the <c>AdditionalCastCostEffect</c> producer. Distinct from
/// <see cref="CounterCountQuantity"/> (counters on an object) and <see cref="CountQuantity"/>
/// (objects matching a filter): "times kicked" is neither a counter nor an object count.
/// </para>
/// </summary>
[OracleQuantity("kickedCount")]
public sealed record KickedCountQuantity : Quantity
{
  /// <summary>
  /// The kicker-family keyword whose times-paid this quantity equals. Always
  /// <see cref="KeywordAbility.Kicker"/> — a multikicker cost is a kicker cost
  /// (CR 702.33c), so "for each time it was kicked" references the Kicker ability.
  /// </summary>
  public required KeywordAbility Keyword { get; init; }
}

/// <summary>
/// A quantity representing "up to N" choices.
/// e.g., "discard up to two cards"
/// </summary>
[OracleQuantity("upTo")]
public sealed record UpToQuantity : Quantity
{
  /// <summary>
  /// The maximum value (N in "up to N").
  /// </summary>
  public required int Maximum { get; init; }

  /// <summary>
  /// The minimum value (usually 0, but can be different).
  /// </summary>
  public int Minimum { get; init; }
}

/// <summary>
/// A quantity derived from a calculation or expression.
/// e.g., "half X rounded down", "twice that many", "+2 for each Aura attached".
///
/// <para>
/// A calculated quantity carries EITHER a structured <see cref="Operand"/>
/// (a simple "multiply the base count by N") OR a free-text
/// <see cref="Expression"/> for shapes not yet structured. Exactly one is
/// expected; both are optional so each authoring site can use whichever fits.
/// </para>
/// </summary>
[OracleQuantity("calculated")]
public sealed record CalculatedQuantity : Quantity
{
  /// <summary>
  /// Free-text description of the calculation, for shapes not expressed by the
  /// structured <see cref="Operand"/>. Optional.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Expression { get; init; }

  /// <summary>
  /// The structured scalar operand for a simple arithmetic operation —
  /// e.g. <c>Operation="multiply", Operand=2</c> for "+2 for each …".
  /// Optional; mutually exclusive with <see cref="Expression"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Operand { get; init; }

  /// <summary>
  /// The base quantity being modified (optional).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? BaseQuantity { get; init; }

  /// <summary>
  /// The operation: half, double, triple, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Operation { get; init; }

  /// <summary>
  /// Rounding mode if applicable: up, down, none.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Rounding { get; init; }
}
