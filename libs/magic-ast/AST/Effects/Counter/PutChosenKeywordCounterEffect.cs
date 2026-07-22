namespace MagicAST.AST.Effects.Counter;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "put your choice of a [keyword], [keyword], or [keyword] counter on [target]" — the
/// controller places one counter whose KIND they choose from an enumerated, closed menu of
/// named keyword-counter types (T-45 Power Armor: "put your choice of a menace, trample, or
/// lifelink counter on it").
///
/// <para>
/// Distinct from <see cref="PutCountersEffect"/>, whose <see cref="PutCountersEffect.CounterType"/>
/// is a single statically-fixed string: here the counter kind is a runtime choice, but — unlike an
/// open "of any kind" domain — the choice is restricted to a printed, enumerated SUBSET
/// (<see cref="Options"/>). Distinct also from
/// <see cref="PutAdditionalCounterOfChosenKindEffect"/>, where the chooser picks a kind already
/// present on the permanent rather than from a printed menu. The enumerated options are typed
/// <see cref="KeywordAbility"/> identities (CR 122.1e — keyword counters: a "flying counter"
/// grants flying, etc.; CR 702).
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed menu and that the controller
/// chooses one; the engine performs the choice and applies the granted keyword.
/// </para>
/// </summary>
[OracleEffect("putChosenKeywordCounter")]
public sealed record PutChosenKeywordCounterEffect : Effect
{
  /// <summary>The permanent the chosen counter is placed on.</summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The closed, enumerated menu of keyword-counter kinds the controller chooses ONE of, in
  /// printed order ("menace, trample, or lifelink" → [Menace, Trample, Lifelink]).
  /// </summary>
  public required IReadOnlyList<KeywordAbility> Options { get; init; }

  /// <summary>
  /// How many counters of the chosen kind are placed. Literal 1 for the singular "a … counter"
  /// (the only printed form so far); a first-class quantity for parity with the other counter nodes.
  /// </summary>
  public required Quantity Count { get; init; }
}
