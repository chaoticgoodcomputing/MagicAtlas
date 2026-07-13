namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal the top card of your library. If it's a [filter] card, put it onto the
/// battlefield. Otherwise, put it into your graveyard." — the Call of the Wild
/// reveal-top-card-and-partition family.
///
/// <para>
/// This is a single coupled atomic action: the controller reveals the SINGLE top
/// card of their library (CR 701.20 — Reveal: "To show a card to all players for a
/// brief time"), and that same revealed card is then routed to exactly one of two
/// destinations by whether it matches <see cref="Filter"/> — if it matches it is put
/// onto the battlefield (CR 400.7, the card changes zones and becomes a new object),
/// otherwise it is put into the controller's graveyard (CR 404.1, a graveyard is the
/// discard pile).
/// </para>
///
/// <para>
/// The three-sentence oracle text is one coupled action — "it" in the second and
/// third sentences back-references the single card revealed in the first. This MUST
/// NOT be decomposed into separate reveal + conditional zone-change effects (the "it"
/// binding would dangle), mirroring the coupled precedent of the neighbouring
/// reveal-top families (<see cref="RevealTopPutMatchingToHandEffect"/>,
/// <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>,
/// <see cref="TopLookPutOntoBattlefieldEffect"/>).
/// </para>
///
/// <para>
/// A DISTINCT shape from those neighbours — do not collapse:
/// <list type="bullet">
///   <item><see cref="RevealTopPutMatchingToHandEffect"/> / <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>
///   reveal a FIXED count of N cards and PARTITION the pile by filter (matching set →
///   hand, remainder → bottom/graveyard).</item>
///   <item><see cref="TopLookPutOntoBattlefieldEffect"/> LOOKS (doesn't reveal), keeps
///   an optional up-to-one matching card, rest to the bottom.</item>
/// </list>
/// This effect instead REVEALS the SINGLE top card and routes THAT card by a static
/// <see cref="ObjectFilter"/> to either the battlefield (match) or the graveyard
/// (non-match) — both dispositions baked into this discriminator's meaning, following
/// the minimal-fields precedent of the sibling reveal-top effects (no destination enum
/// is added to the record).
/// </para>
/// </summary>
[OracleEffect("revealTopCardMatchToBattlefieldElseGraveyard")]
public sealed record RevealTopCardMatchToBattlefieldElseGraveyardEffect : Effect
{
  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the
  /// controller of "your library").
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The filter the revealed top card must match to be put onto the battlefield —
  /// "a creature card" → <c>{ CardTypes: ["creature"] }</c>. When the revealed card
  /// does NOT match this filter, it is put into the controller's graveyard instead.
  /// </summary>
  public required ObjectFilter Filter { get; init; }
}
