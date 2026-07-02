namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal the top N cards of your library. Put all [filter] cards revealed this
/// way into your hand and the rest on the bottom of your library in any order." —
/// the Sylvan Messenger ETB reveal-and-partition family.
///
/// <para>
/// This is a single coupled atomic action (CR 701.20 — Reveal: "To show a card to
/// all players for a brief time"): the controller reveals a FIXED number of cards
/// from the top of their library, ALL of the revealed cards matching
/// <see cref="Filter"/> go to the controller's hand (zero, some, or all of the
/// revealed cards may match), and the remaining (non-matching) cards go to the
/// bottom of the library in an order the controller chooses. CR 401.4: "If an
/// effect puts two or more cards in a specific position in a library at the same
/// time, the owner of those cards may arrange them in any order. That library's
/// owner doesn't reveal the order in which the cards go into the library."
/// </para>
///
/// <para>
/// The two-sentence oracle text is one coupled action — "revealed this way" and
/// "the rest" in the second sentence are back-references to the reveal in the
/// first. This MUST NOT be decomposed into separate reveal + zone-change effects.
/// </para>
///
/// <para>
/// This is a DISTINCT shape from the other three-and-four reveal/look families and
/// must not be collapsed into any of them:
/// <list type="bullet">
///   <item><see cref="RevealUntilEffect"/> — reveals UNTIL the first match (not a
///   fixed count), puts exactly ONE card to hand, and the rest go to the bottom in
///   a RANDOM order (CR 400.4).</item>
///   <item><see cref="AbundanceRevealEffect"/> — reveals until a land/nonland
///   choice is met, puts exactly ONE card to hand, rest to bottom in any order.</item>
///   <item><see cref="OracleTopLookEffect"/> — LOOKS (doesn't reveal) at a fixed
///   count, puts UP TO ONE card back on TOP, rest to bottom in a RANDOM order.</item>
///   <item><see cref="ImpulseEffect"/> — LOOKS at a fixed count, puts exactly ONE
///   (controller's free choice) to hand, rest to graveyard or bottom.</item>
/// </list>
/// This effect instead REVEALS a fixed count, puts ALL cards matching a static
/// <see cref="ObjectFilter"/> to hand (not a free choice, not "until" the first
/// match), and puts the non-matching remainder on the bottom in a player-chosen
/// (any) order — a disposition baked into this discriminator's meaning, following
/// the minimal-fields precedent of <see cref="AbundanceRevealEffect"/> (no separate
/// destination enum is added to the record).
/// </para>
/// </summary>
[OracleEffect("revealTopPutMatchingToHand")]
public sealed record RevealTopPutMatchingToHandEffect : Effect
{
  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards from the top of the library are revealed (a fixed number
  /// printed on the card, e.g. four).
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The filter a revealed card must match to go to the controller's hand
  /// (e.g. an Elf card: <c>{ Subtypes: ["Elf"] }</c>). Every revealed card NOT
  /// matching this filter goes to the bottom of the library in any order.
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}
