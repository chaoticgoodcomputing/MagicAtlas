namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal cards from the top of your library until you reveal a [filter] card.
///  Put that card into your hand and the rest on the bottom of your library in a random order."
///
/// <para>
/// This is a single atomic action (CR 701.12 — look/reveal): the controller reveals cards
/// one at a time from the top of their library until they find the first card matching
/// <see cref="Filter"/>, which then goes to their hand. All other revealed cards are placed
/// on the bottom of the library in a random (shuffled) order.
/// </para>
///
/// <para>
/// The two-sentence oracle text is one game action — "that card" in the second sentence is a
/// back-reference to the matching card found in the first. This MUST NOT be decomposed into
/// separate reveal + zone-change effects; the coupling between the found card and the rest is
/// part of the action's meaning (mirroring the coupling in <see cref="ImpulseEffect"/> and
/// <see cref="TopLookPutOntoBattlefieldEffect"/>).
/// </para>
///
/// <para>
/// CR 701.12 (look/reveal from library); CR 400.4 (random order on the bottom).
/// </para>
/// </summary>
[OracleEffect("revealUntil")]
public sealed record RevealUntilEffect : Effect
{
  /// <summary>
  /// The filter a card must match to stop the reveal — the card that goes to hand.
  /// e.g. "an Elf or Elemental card" → <c>{ CardTypes: ["card"], Subtypes: ["Elf", "Elemental"] }</c>.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
