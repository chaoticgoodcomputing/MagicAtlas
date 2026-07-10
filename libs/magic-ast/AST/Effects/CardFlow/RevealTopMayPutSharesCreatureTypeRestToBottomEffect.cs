namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal the top N cards of your library. You may put a card that shares a creature
/// type with it from among them into your hand. Put the rest on the bottom of your
/// library in a random order." — the Tajuru Paragon kicked-ETB reveal family.
///
/// <para>
/// This is a single coupled atomic action (CR 701.20 — Reveal): the controller reveals a
/// FIXED number of cards from the top of their library, MAY put UP TO ONE of the revealed
/// cards that shares a creature type with the source object (<see cref="ObjectFilter.SharesCreatureTypeWith"/>,
/// CR 205.3m) into their hand — an optional, player-chosen selection, not "all matching" —
/// and every remaining revealed card goes to the bottom of the controller's library in a
/// RANDOM order. CR 400.4: cards placed on the bottom of a library "in a random order" are
/// shuffled among themselves before being placed.
/// </para>
///
/// <para>
/// The three-sentence oracle text is one coupled action — "from among them" and "the rest"
/// are back-references to the reveal in the first sentence. This MUST NOT be decomposed
/// into separate reveal + zone-change effects (mirroring the coupled precedent of
/// <see cref="RevealTopPutMatchingToHandEffect"/> and <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>).
/// </para>
///
/// <para>
/// A DISTINCT shape from the neighbouring reveal families — do not collapse:
/// <list type="bullet">
///   <item><see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/> — the non-matching
///   remainder goes to the GRAVEYARD, and the hand-eligible filter is a static
///   <see cref="ObjectFilter"/> (not a relational share-predicate).</item>
///   <item><see cref="RevealTopPutMatchingToHandEffect"/> — puts ALL matching cards to
///   hand (not an optional up-to-one), and the remainder goes to the bottom in ANY
///   (player-chosen) order, not a random one.</item>
///   <item><see cref="RevealUntilEffect"/> — reveals UNTIL the first match (not a fixed
///   count), and the match is MANDATORY (not "you may").</item>
/// </list>
/// This effect instead REVEALS a fixed count, OPTIONALLY keeps up to one card sharing a
/// creature type with the referenced object, and sends the non-kept remainder to the
/// BOTTOM of the library in a RANDOM order — a disposition baked into this discriminator's
/// meaning, following the minimal-fields precedent of <see cref="RevealTopPutMatchingToHandEffect"/>.
/// </para>
/// </summary>
[OracleEffect("revealTopMayPutSharesCreatureTypeRestToBottom")]
public sealed record RevealTopMayPutSharesCreatureTypeRestToBottomEffect : Effect
{
  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards from the top of the library are revealed (a fixed number printed on
  /// the card, e.g. six).
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The filter a revealed card must match to be eligible to go to the controller's
  /// hand — a relational <see cref="ObjectFilter.SharesCreatureTypeWith"/> predicate
  /// keyed on the source object (<c>{Kind: Self}</c>). The controller MAY put up to one
  /// matching card into hand; every card not put into hand goes to the bottom of the
  /// library in a random order.
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}
