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
public sealed record ImpulseEffect : ContinuousEffect
{
  /// <summary>
  /// How many cards to look at from the top of the library.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// Where the unchosen cards go after the controller keeps one.
  /// </summary>
  public required ImpulseRestDestination RestDestination { get; init; }

  /// <summary>
  /// Restricts <em>which</em> of the exiled cards may be played/cast during the
  /// <see cref="ContinuousEffect.Duration"/> window, for the
  /// <see cref="ImpulseRestDestination.RemainExiled"/> shape — "you may cast an
  /// <b>instant or sorcery</b> spell from among those exiled cards" (Chandra,
  /// Hope's Beacon's first +1: <c>CardTypes = ["instant","sorcery"]</c>). A
  /// structured <see cref="ObjectFilter"/>, NOT a free-text note. Null for the
  /// un-restricted "you may play those cards" family (Jeska's Will, The Legend of
  /// Roku), where every exiled card is playable.
  ///
  /// <para>
  /// CR 601.3e: an effect may grant permission to cast a card from a zone other
  /// than the hand, with any stated type restriction. Reference-not-resolution
  /// (ADR 0004): MAST records which exiled cards qualify; the engine enforces the
  /// permission at cast time.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public MagicAST.AST.References.ObjectFilter? PlayableFilter { get; init; }

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

  /// <summary>
  /// The cards remain in exile and stay playable for a stated window — "exile the
  /// top N cards … you may play those cards until [time]" (The Legend of Roku I).
  /// Here all N cards are exiled (none kept in hand) and the <see cref="Effect"/>'s
  /// inherited <see cref="ContinuousEffect.Duration"/> bounds the play window.
  /// </summary>
  RemainExiled,
}
