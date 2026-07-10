namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Reveal cards from the top of your library until you reveal a [filter] card. Put
/// that card into your hand and exile all other cards revealed this way." — the
/// Demonic Consultation / Tainted Pact "hellbent tutor" shape.
///
/// <para>
/// This is a single coupled atomic action (CR 701.20a — reveal): the controller
/// reveals cards one at a time from the top of their library until they find the
/// first card matching <see cref="Filter"/>, which then goes to their hand (CR
/// 701.13a — exile). Every other card revealed during the search is exiled, not
/// returned to the library — a materially different disposition of the "miss" pile
/// from the sibling <see cref="RevealUntilEffect"/>.
/// </para>
///
/// <para>
/// The two-sentence oracle text is one game action — "that card" and "all other
/// cards revealed this way" in the second sentence back-reference the search in the
/// first. This MUST NOT be decomposed into separate reveal + zone-change effects
/// (mirroring the coupling convention of <see cref="RevealUntilEffect"/>,
/// <see cref="ImpulseEffect"/>, and <see cref="TopLookPutOntoBattlefieldEffect"/>).
/// </para>
///
/// <para>
/// Sibling of <see cref="RevealUntilEffect"/> — SAME reveal-until-first-match search,
/// DIFFERENT rest disposition: <see cref="RevealUntilEffect"/> puts the non-matching
/// remainder on the BOTTOM of the library in a random order (CR 400.4); this effect
/// EXILES the entire remainder instead (no shuffle, no return to the library — the
/// library may be fully emptied by the search). The differing disposition is baked
/// into the discriminator rather than added as a destination enum, following the
/// established precedent of the neighbouring reveal-family nodes (e.g.
/// <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/> vs.
/// <see cref="RevealTopPutMatchingToHandEffect"/>).
/// </para>
///
/// <para>
/// CR 701.20a (reveal); CR 701.13a (exile).
/// </para>
/// </summary>
[OracleEffect("revealUntilExileRest")]
public sealed record RevealUntilExileRestEffect : Effect
{
  /// <summary>
  /// The filter a card must match to stop the reveal — the card that goes to hand.
  /// For "a card with the chosen name" (Demonic Consultation), this is
  /// <c>{ CardTypes: ["card"], ChosenCharacteristic: CardName }</c> — the structured
  /// consumer of a preceding <see cref="Keyword.ChooseCardNameEffect"/> declaration
  /// (CR 607 linked ability), mirroring <see cref="ChosenCharacteristicKind.CardName"/>'s
  /// use elsewhere (e.g. <c>CounterTargetSpellWithChosenNameActivatedEffectRule</c>).
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
