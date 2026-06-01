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
/// A quantity equal to the number of times a keyword's repeatable additional cost was paid
/// as the spell was cast — "for each time it was kicked" (Wolfbriar Elemental, keyed on
/// Kicker: CR 702.33d defines the kicked state and 702.33c folds a multikicker cost into a
/// kicker cost). The count sibling of <see cref="MagicAST.AST.Abilities.KeywordCostPaidCondition"/>
/// (the boolean was-it-paid): this carries the times-paid for a repeatable keyword
/// (Kicker/Multikicker, and prospectively Squad/Replicate).
///
/// <para>
/// Reference-not-resolution (ADR 0004): keyed on the producing keyword's typed
/// <see cref="KeywordAbility"/> identity (a linked ability, e.g. CR 702.33e), NOT a
/// variable threaded from the <c>AdditionalCastCostEffect</c> producer. Distinct from
/// <see cref="CounterCountQuantity"/> (counters on an object) and <see cref="CountQuantity"/>
/// (objects matching a filter): a times-paid count is neither a counter nor an object count.
/// </para>
/// </summary>
[OracleQuantity("keywordCostPaidCount")]
public sealed record KeywordCostPaidCountQuantity : Quantity
{
  /// <summary>
  /// The keyword whose times-paid this quantity equals — Kicker (a multikicker cost is a
  /// kicker cost, CR 702.33c, so "for each time it was kicked" references Kicker), and
  /// prospectively other repeatable additional-cost keywords (Squad, Replicate).
  /// </summary>
  public required KeywordAbility Keyword { get; init; }
}

/// <summary>
/// A quantity equal to the number of counters <b>removed this way</b> by the
/// activation cost of the same ability — "Add {G} for each storage counter
/// removed this way" (Hollow Trees), "add an additional {B} for each charge
/// counter removed this way" (Black Mana Battery). The cost-linked sibling of
/// <see cref="CounterCountQuantity"/> (counters currently <i>on</i> an object)
/// and <see cref="KeywordCostPaidCountQuantity"/> (a keyword cost paid): a count
/// of counters consumed by this ability's own <see cref="MagicAST.AST.Costs.RemoveCountersCost"/>.
///
/// <para>
/// Reference-not-resolution (ADR 0004): "this way" names the
/// <see cref="MagicAST.AST.Costs.RemoveCountersCost"/> on the <i>same</i> ability,
/// it is NOT a variable threaded from the cost — MAST records the textual link,
/// not the runtime value (counter mechanics are engine territory; CR 122.1 — "A
/// counter is a marker placed on an object or player … Counters are not objects
/// and have no characteristics"). The kind of counter removed is the one axis.
/// </para>
/// </summary>
[OracleQuantity("countersRemovedThisWay")]
public sealed record CountersRemovedThisWayQuantity : Quantity
{
  /// <summary>
  /// The counter kind removed by the cost, e.g. "storage", "charge".
  /// </summary>
  public required string CounterType { get; init; }
}

/// <summary>
/// The cost choice "Remove <b>any number of</b> [type] counters" — an unbounded
/// player choice (Hollow Trees, the Mana Batteries). Per CR 107.3 — "Many objects
/// use the letter X as a placeholder for a number that needs to be determined …
/// the rest let their controller choose the value of X" — this is a free,
/// upper-unbounded choice, distinct from <see cref="UpToQuantity"/> (a bounded
/// "up to N") and <see cref="VariableQuantity"/> (a named X with a defined value).
/// Field-less; reusable for any "any number of" text.
/// </summary>
[OracleQuantity("anyAmount")]
public sealed record AnyAmountQuantity : Quantity;

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
