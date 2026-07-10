namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Players play with their hands revealed." (Revelation) / "Your opponents play
/// with their hands revealed." (Telepathy) — a continuous static effect that
/// requires the affected player(s) to keep every card in their hand face-up and
/// visible to all players at all times while the source permanent is on the
/// battlefield.
///
/// <para>
/// CR 701.20a (verbatim): "To reveal a card, show that card to all players for a
/// brief time. If an effect causes a card to be revealed, it remains revealed for
/// as long as necessary to complete the parts of the effect that card is relevant
/// to. If the cost to cast a spell or activate an ability includes revealing a
/// card, or if a card is revealed because an ability is activated from a hidden
/// zone (see rule 602.2a), the card remains revealed from the time the spell or
/// ability is announced until the time it leaves the stack. If revealing a card
/// causes a triggered ability to trigger, the card remains revealed until that
/// triggered ability leaves the stack. If that ability isn't put onto the stack
/// the next time a player would receive priority, the card ceases to be
/// revealed." The CR 701.20 rule text itself uses Telepathy as its worked example
/// of this exact "play with their hands revealed" phrasing.
/// </para>
///
/// <para>
/// MAST encodes this as a continuous static effect (no <c>Duration</c> stated —
/// the effect persists as long as the source permanent is on the battlefield,
/// per CR 604.2), mirroring the sibling library-transparency effect
/// <see cref="PlayWithTopRevealedEffect"/>. The imperative "Players play with …"
/// / "Your opponents play with …" form is NOT optional: the affected player(s)
/// have no choice. No <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>
/// wrapper is applied.
/// </para>
///
/// <para>
/// <see cref="Player"/> generalizes the player scope so this one node covers both
/// printed shapes: "Players play with their hands revealed" (Revelation) →
/// <c>{ Kind: EachPlayer }</c>; "Your opponents play with their hands revealed"
/// (Telepathy) → <c>{ Kind: EachOpponent }</c>.
/// </para>
/// </summary>
[OracleEffect("playWithHandsRevealed")]
public sealed record PlayWithHandsRevealedEffect : Effect
{
  /// <summary>
  /// Which player(s) must play with their hand revealed. "Players play with
  /// their hands revealed" → <c>{ Kind: EachPlayer }</c>; "Your opponents play
  /// with their hands revealed" → <c>{ Kind: EachOpponent }</c>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
