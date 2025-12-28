namespace MagicAST.AST.Costs;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Base type for all costs in Magic.
/// Costs are what must be paid to cast spells or activate abilities.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "costType")]
[JsonDerivedType(typeof(ManaCost), "mana")]
[JsonDerivedType(typeof(TapCost), "tap")]
[JsonDerivedType(typeof(UntapCost), "untap")]
[JsonDerivedType(typeof(SacrificeCost), "sacrifice")]
[JsonDerivedType(typeof(DiscardCost), "discard")]
[JsonDerivedType(typeof(PayLifeCost), "payLife")]
[JsonDerivedType(typeof(ExileCost), "exile")]
[JsonDerivedType(typeof(RemoveCountersCost), "removeCounters")]
[JsonDerivedType(typeof(TapPermanentsCost), "tapPermanents")]
[JsonDerivedType(typeof(CompositeCost), "composite")]
public abstract record Cost;

/// <summary>
/// A mana cost like "{2}{G}{G}" or "{X}{R}".
/// </summary>
public sealed record ManaCost : Cost
{
  /// <summary>
  /// The mana symbols in this cost.
  /// </summary>
  [JsonPropertyName("symbols")]
  public required IReadOnlyList<ManaSymbol> Symbols { get; init; }
}

/// <summary>
/// The tap symbol {T}.
/// </summary>
public sealed record TapCost : Cost
{
  /// <summary>
  /// What to tap. Null means tap this permanent.
  /// </summary>
  [JsonPropertyName("target")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// The untap symbol {Q}.
/// </summary>
public sealed record UntapCost : Cost
{
  /// <summary>
  /// What to untap. Null means untap this permanent.
  /// </summary>
  [JsonPropertyName("target")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// "Sacrifice a [filter]" or "Sacrifice X [filter]s".
/// </summary>
public sealed record SacrificeCost : Cost
{
  /// <summary>
  /// What must be sacrificed.
  /// </summary>
  [JsonPropertyName("filter")]
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be sacrificed.
  /// </summary>
  [JsonPropertyName("quantity")]
  public required Quantity Quantity { get; init; }
}

/// <summary>
/// "Discard a card" or "Discard X cards".
/// </summary>
public sealed record DiscardCost : Cost
{
  /// <summary>
  /// What must be discarded.
  /// </summary>
  [JsonPropertyName("filter")]
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be discarded.
  /// </summary>
  [JsonPropertyName("quantity")]
  public required Quantity Quantity { get; init; }
}

/// <summary>
/// "Pay N life".
/// </summary>
public sealed record PayLifeCost : Cost
{
  /// <summary>
  /// How much life to pay.
  /// </summary>
  [JsonPropertyName("amount")]
  public required Quantity Amount { get; init; }
}

/// <summary>
/// "Exile [filter] from [zone]".
/// </summary>
public sealed record ExileCost : Cost
{
  /// <summary>
  /// What must be exiled.
  /// </summary>
  [JsonPropertyName("filter")]
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be exiled.
  /// </summary>
  [JsonPropertyName("quantity")]
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// The zone to exile from.
  /// </summary>
  [JsonPropertyName("fromZone")]
  public required Zone FromZone { get; init; }
}

/// <summary>
/// "Remove N counters from [target]".
/// </summary>
public sealed record RemoveCountersCost : Cost
{
  /// <summary>
  /// The type of counter to remove.
  /// </summary>
  [JsonPropertyName("counterType")]
  public required string CounterType { get; init; }

  /// <summary>
  /// How many counters to remove.
  /// </summary>
  [JsonPropertyName("quantity")]
  public required Quantity Quantity { get; init; }

  /// <summary>
  /// What to remove counters from.
  /// </summary>
  [JsonPropertyName("target")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}

/// <summary>
/// "Tap N untapped [filter]s you control".
/// </summary>
public sealed record TapPermanentsCost : Cost
{
  /// <summary>
  /// What must be tapped.
  /// </summary>
  [JsonPropertyName("filter")]
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// How many must be tapped.
  /// </summary>
  [JsonPropertyName("quantity")]
  public required Quantity Quantity { get; init; }
}

/// <summary>
/// Multiple costs combined with commas.
/// e.g., "{2}{B}, {T}, Sacrifice a creature"
/// </summary>
public sealed record CompositeCost : Cost
{
  /// <summary>
  /// The individual costs that make up this composite cost.
  /// </summary>
  [JsonPropertyName("costs")]
  public required IReadOnlyList<Cost> Costs { get; init; }
}
