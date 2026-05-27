namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Look at the top N cards of your library. Put one of them into your hand
/// and the rest into [RestDestination]."
///
/// Models the Impulse/Strategic Planning family: a single atomic action where
/// the controller looks at the top N cards, chooses one to keep in hand, and
/// the remaining cards go to a fixed destination. Rule 701.12 (look).
///
/// <para>
/// The two-sentence oracle text is a single game action — "the rest" in the
/// second sentence is a back-reference to the cards revealed by the first.
/// This must not be decomposed into a flat [lookAtCards, …] list; a single
/// ImpulseEffect preserves that semantic coupling.
/// </para>
///
/// <para>Variants by RestDestination:
/// <list type="bullet">
///   <item><see cref="ImpulseRestDestination.Graveyard"/> — Strategic Planning, Rummage</item>
///   <item><see cref="ImpulseRestDestination.BottomOfLibrary"/> — Impulse (Visions), Anticipate</item>
/// </list>
/// </para>
/// </summary>
[OracleEffect("impulse")]
public sealed record ImpulseEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// How many cards to look at from the top of the library.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// Where the unchosen cards go after the controller keeps one.
  /// </summary>
  public required ImpulseRestDestination RestDestination { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}

/// <summary>
/// Where the unchosen cards go in an <see cref="ImpulseEffect"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImpulseRestDestination
{
  /// <summary>The unchosen cards go to the graveyard (Strategic Planning, Rummage).</summary>
  Graveyard,

  /// <summary>The unchosen cards go to the bottom of the library in any order (Impulse, Anticipate).</summary>
  BottomOfLibrary,
}
