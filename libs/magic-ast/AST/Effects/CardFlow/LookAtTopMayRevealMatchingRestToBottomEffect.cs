namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Look at the top N cards of your library. You may reveal a [filter] card from
/// among them and put it into your hand. Put the rest on the bottom of your
/// library in a random order." — Dragonologist's ETB pattern.
///
/// <para>
/// This is a single coupled atomic action: the controller privately LOOKS
/// (looking at cards is a general game instruction, not one of the CR 701
/// keyword actions) at a FIXED number of cards from the top of their library —
/// unlike the sibling <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>
/// and <see cref="RevealTopMayPutSharesCreatureTypeRestToBottomEffect"/>
/// families, this pile is never shown to any other player. The controller MAY
/// then reveal (CR 701.20a: "to reveal a card means to show it to all
/// players") UP TO ONE of the looked-at cards matching <see cref="Filter"/>
/// and put it into their hand — an optional, player-chosen selection, not
/// "all matching" — and every remaining looked-at card goes to the bottom of
/// the library in a RANDOM order without ever having been shown to anyone.
/// CR 401.4: cards placed at a specific position in a library "in a random
/// order" are shuffled among themselves before being placed.
/// </para>
///
/// <para>
/// The three-sentence oracle text is one coupled action — "from among them"
/// and "the rest" are back-references to the look in the first sentence. This
/// MUST NOT be decomposed into separate look + reveal + zone-change effects.
/// </para>
///
/// <para>
/// A DISTINCT shape from the neighbouring look/reveal families — do not collapse:
/// <list type="bullet">
///   <item><see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/> — REVEALS
///   (not merely looks at) the whole pile up front to every player, and the
///   non-kept remainder goes to the GRAVEYARD, not the bottom of the library.</item>
///   <item><see cref="RevealTopMayPutSharesCreatureTypeRestToBottomEffect"/> —
///   also REVEALS the whole pile up front (public information), and the
///   hand-eligible filter is a relational <c>SharesCreatureTypeWith</c>
///   predicate rather than a static <see cref="ObjectFilter"/>.</item>
///   <item><see cref="ImpulseEffect"/> — LOOKS (matching this effect), but
///   keeps EXACTLY ONE card with NO type filter (a free choice among all N),
///   and sends the rest to a fixed <see cref="ImpulseRestDestination"/>
///   (never "random order").</item>
///   <item><see cref="RevealUntilEffect"/> — reveals UNTIL the first match
///   (not a fixed count), and the match is MANDATORY (not "you may").</item>
/// </list>
/// This effect instead LOOKS PRIVATELY at a fixed count, OPTIONALLY reveals and
/// keeps up to one card matching a static <see cref="ObjectFilter"/>, and sends
/// the non-kept remainder to the BOTTOM of the library in a RANDOM order — a
/// disposition baked into this discriminator's meaning, following the
/// minimal-fields precedent of <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>.
/// </para>
/// </summary>
[OracleEffect("lookAtTopMayRevealMatchingRestToBottom")]
public sealed record LookAtTopMayRevealMatchingRestToBottomEffect : Effect
{
  /// <summary>
  /// The player performing the look — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards from the top of the library are looked at (a fixed number
  /// printed on the card, e.g. six).
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The filter a looked-at card must match to be eligible for the optional
  /// reveal-and-hand action — "an instant, sorcery, or Dragon card" →
  /// <c>{ CardTypes: ["instant", "sorcery"], Subtypes: ["Dragon"] }</c> (the
  /// established mixed type-or-subtype disjunction shape, matching
  /// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.CounterTargetTypeOrSubtypeSpellRule"/>'s
  /// "creature or Aura" filter construction). The controller MAY reveal and put up to one
  /// matching card into hand; every card not put into hand goes to the bottom
  /// of the library in a random order, unrevealed.
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}
