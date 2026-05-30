namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.AST.References;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A predicate over game state — "you control a Forest", "seven or more cards in
/// your graveyard", "enchanted creature is black". Modelled as one discriminated
/// union that <i>composes the existing primitives</i> (<see cref="ObjectFilter"/>,
/// <see cref="Comparison"/>, and — in later arms — <c>HistoryPredicate</c>)
/// rather than introducing a new one. The single home for every "if …",
/// "as long as …", and "unless …"-style predicate, replacing the former stringly
/// <c>Condition { Text }</c> record and the bare <c>string Condition</c> fields.
///
/// <para>
/// Encoded as written; the engine evaluates it against game state — a
/// <see cref="CountCondition"/> is "you control a Forest", never a pre-resolved
/// boolean (ADR 0004, reference-not-resolution). Seeded worst-first (ADR 0001):
/// <see cref="CountCondition"/> for the dominant shape and the
/// <see cref="OtherCondition"/> residual for the rest; history, object-state, and
/// compound arms are added as the card families that need them land. See ADR 0007.
/// </para>
/// </summary>
[PolymorphicBase("ConditionType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Condition>))]
public abstract record Condition
{
  /// <summary>The typed residual for a not-yet-structured condition phrase.</summary>
  public static OtherCondition Other(string text) => new() { Text = text };
}

/// <summary>
/// A count of objects matching a filter, compared to a threshold — the dominant
/// condition shape ("you control two or more artifacts", "seven or more cards in
/// your graveyard", "two or fewer other lands"). Composes <see cref="ObjectFilter"/>
/// (which objects — controller, zone, types) and <see cref="Comparison"/>
/// (the threshold).
/// </summary>
[ConditionKind("count")]
public sealed record CountCondition : Condition
{
  /// <summary>Which objects are counted — controller, zone, types, etc.</summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>The threshold the count is compared against.</summary>
  public required Comparison Count { get; init; }
}

/// <summary>
/// Typed residual for a condition that does not yet have a structured variant —
/// carries the literal oracle phrase. A deferral, not a destination (ADR 0001):
/// counted by the residual-debt metric, structured when the shape recurs.
/// </summary>
[ConditionKind("other")]
public sealed record OtherCondition : Condition, IResidual
{
  /// <summary>The literal condition phrase from the oracle text.</summary>
  public required string Text { get; init; }
}
