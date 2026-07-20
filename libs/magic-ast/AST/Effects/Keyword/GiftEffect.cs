namespace MagicAST.AST.Effects.Keyword;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// The payoff of the Gift keyword (CR 702.174) on an instant or sorcery spell —
/// "Gift a [something]". CR 702.174b: on an instant/sorcery the second ability
/// represented by gift is "If this spell's gift cost was paid, [effect]", and CR
/// 702.174e: "'Gift a card' means the effect is 'The chosen player draws a card.'"
/// This node carries the parameterized gifted effect in <see cref="Gifted"/> — for
/// "Gift a card" a <see cref="MagicAST.AST.Effects.CardFlow.DrawCardsEffect"/> whose
/// <c>Player</c> is the chosen player (<see cref="MagicAST.AST.References.ControllerFilter.ChosenPlayer"/>).
///
/// <para>The "if the gift was promised" gate and the "before any other spell abilities
/// of the card" timing (CR 702.174j) are inherent to the gift keyword and so are not
/// re-encoded as fields — the node's very presence, paired with its enclosing keyword
/// ability's <see cref="MagicAST.AST.References.KeywordAbility.Gift"/> source and the
/// verbatim reminder text, records them (MAST describes, does not execute — ADR 0004).
/// The distinct "choose an opponent" additional cost (CR 702.174a: "As an additional
/// cost to cast this spell, you may choose an opponent.") is modelled separately as an
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/> wrapping a
/// <see cref="ChoosePlayerEffect"/> on the same keyword ability.</para>
/// </summary>
[OracleEffect(
  "gift",
  NearDuplicateOf = new[] { "graft" },
  Reason = "Unrelated keyword abilities that happen to sit at Levenshtein 2. Gift (CR 702.174) is an additional-cost/payoff pair on an instant or sorcery; Graft N (CR 702.58) is a +1/+1-counter keyword on a permanent. No shared mechanic, no shared stem, no consolidation possible. A coincidence of spelling, not sprawl."
)]
public sealed record GiftEffect : Effect
{
  /// <summary>
  /// The effect the chosen player receives when the gift resolves — CR 702.174b/e.
  /// For "Gift a card" this is a draw of one card by the chosen player.
  /// </summary>
  public required Effect Gifted { get; init; }
}
