namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal the top N cards of your library. You may put a [filter] card from among
/// them into your hand. Put the rest into your graveyard." — the Grisly
/// Salvage / Scout the Borders / Commune with the Gods reveal-then-dredge family.
///
/// <para>
/// This is a single coupled atomic action (CR 701.20 — Reveal: "To show a card to
/// all players for a brief time"): the controller reveals a FIXED number of cards
/// from the top of their library, MAY put UP TO ONE of the revealed cards matching
/// <see cref="Filter"/> into their hand (an optional, player-chosen selection — not
/// "all matching", not "until" the first match), and every remaining revealed card
/// goes to the controller's graveyard. CR 404.1 (a graveyard is the discard pile);
/// CR 404.3: "If an effect or rule puts two or more cards into the same graveyard at
/// the same time, the owner of those cards may arrange them in any order."
/// </para>
///
/// <para>
/// The three-sentence oracle text is one coupled action — "from among them" and
/// "the rest" are back-references to the reveal in the first sentence. This MUST
/// NOT be decomposed into separate reveal + zone-change effects (mirroring the
/// coupled precedent of <see cref="RevealTopPutMatchingToHandEffect"/>).
/// </para>
///
/// <para>
/// A DISTINCT shape from the neighbouring reveal/look families — do not collapse:
/// <list type="bullet">
///   <item><see cref="RevealTopPutMatchingToHandEffect"/> — puts ALL matching cards
///   to hand (not an optional up-to-one) and the remainder on the BOTTOM of the
///   library in any order (Sylvan Messenger).</item>
///   <item><see cref="ImpulseEffect"/> — LOOKS (doesn't reveal), keeps EXACTLY ONE
///   card with NO type filter, and sends the rest to a
///   <see cref="ImpulseRestDestination"/> (Strategic Planning).</item>
///   <item><see cref="RevealUntilEffect"/> — reveals UNTIL the first match, keeps
///   exactly one, rest to bottom in a random order.</item>
/// </list>
/// This effect instead REVEALS a fixed count, OPTIONALLY keeps up to one card
/// matching a static <see cref="ObjectFilter"/>, and sends the non-kept remainder to
/// the controller's GRAVEYARD — a disposition baked into this discriminator's meaning
/// (following the minimal-fields precedent of
/// <see cref="RevealTopPutMatchingToHandEffect"/>, which likewise bakes its
/// bottom-of-library disposition into the discriminator rather than adding a
/// destination enum).
/// </para>
/// </summary>
[OracleEffect("revealTopMayPutMatchingRestToGraveyard")]
public sealed record RevealTopMayPutMatchingRestToGraveyardEffect : Effect
{
  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards from the top of the library are revealed (a fixed number
  /// printed on the card, e.g. five).
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The filter a revealed card must match to be eligible to go to the
  /// controller's hand — "a creature or land card" → <c>{ CardTypes: ["creature",
  /// "land"] }</c>. The controller MAY put up to one matching card into hand; every
  /// card not put into hand goes to the graveyard.
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}
