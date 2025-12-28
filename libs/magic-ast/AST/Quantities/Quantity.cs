namespace MagicAST.AST.Quantities;

using System.Text.Json.Serialization;

/// <summary>
/// Base type for numeric quantities that may be literal, variable, or derived.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "quantityType")]
[JsonDerivedType(typeof(LiteralQuantity), "literal")]
[JsonDerivedType(typeof(VariableQuantity), "variable")]
[JsonDerivedType(typeof(DerivedQuantity), "derived")]
[JsonDerivedType(typeof(CountQuantity), "count")]
[JsonDerivedType(typeof(UpToQuantity), "upTo")]
[JsonDerivedType(typeof(CalculatedQuantity), "calculated")]
public abstract record Quantity;

/// <summary>
/// A literal numeric value like 1, 2, 3.
/// </summary>
public sealed record LiteralQuantity : Quantity
{
  /// <summary>
  /// The literal value.
  /// </summary>
  [JsonPropertyName("value")]
  public required int Value { get; init; }

  /// <summary>
  /// Creates a literal quantity.
  /// </summary>
  public static LiteralQuantity Of(int value) => new() { Value = value };
}

/// <summary>
/// A variable quantity like X, Y, Z.
/// </summary>
public sealed record VariableQuantity : Quantity
{
  /// <summary>
  /// The variable name (X, Y, Z).
  /// </summary>
  [JsonPropertyName("name")]
  public required string Name { get; init; }

  public static VariableQuantity X => new() { Name = "X" };
  public static VariableQuantity Y => new() { Name = "Y" };
  public static VariableQuantity Z => new() { Name = "Z" };
}

/// <summary>
/// A quantity derived from a characteristic of an object.
/// e.g., "equal to its power", "equal to the number of cards in your hand"
/// </summary>
public sealed record DerivedQuantity : Quantity
{
  /// <summary>
  /// What characteristic the value is derived from.
  /// </summary>
  [JsonPropertyName("derivedFrom")]
  public required DerivedKind DerivedFrom { get; init; }

  /// <summary>
  /// The source object for the derivation.
  /// </summary>
  [JsonPropertyName("source")]
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
/// e.g., "the number of creatures you control"
/// </summary>
public sealed record CountQuantity : Quantity
{
  /// <summary>
  /// What to count.
  /// </summary>
  [JsonPropertyName("countOf")]
  public required string CountOf { get; init; }

  /// <summary>
  /// Optional filter text.
  /// </summary>
  [JsonPropertyName("filter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Filter { get; init; }
}

/// <summary>
/// A quantity representing "up to N" choices.
/// e.g., "discard up to two cards"
/// </summary>
public sealed record UpToQuantity : Quantity
{
  /// <summary>
  /// The maximum value (N in "up to N").
  /// </summary>
  [JsonPropertyName("maximum")]
  public required int Maximum { get; init; }

  /// <summary>
  /// The minimum value (usually 0, but can be different).
  /// </summary>
  [JsonPropertyName("minimum")]
  public int Minimum { get; init; }
}

/// <summary>
/// A quantity derived from a calculation or expression.
/// e.g., "half X rounded down", "twice that many"
/// </summary>
public sealed record CalculatedQuantity : Quantity
{
  /// <summary>
  /// The expression describing the calculation.
  /// </summary>
  [JsonPropertyName("expression")]
  public required string Expression { get; init; }

  /// <summary>
  /// The base quantity being modified (optional).
  /// </summary>
  [JsonPropertyName("baseQuantity")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? BaseQuantity { get; init; }

  /// <summary>
  /// The operation: half, double, triple, etc.
  /// </summary>
  [JsonPropertyName("operation")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Operation { get; init; }

  /// <summary>
  /// Rounding mode if applicable: up, down, none.
  /// </summary>
  [JsonPropertyName("rounding")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Rounding { get; init; }
}
