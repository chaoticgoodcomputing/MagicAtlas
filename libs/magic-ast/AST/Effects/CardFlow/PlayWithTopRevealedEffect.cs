namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Play with the top card of your library revealed." — a continuous static
/// effect that requires the controller to keep the top card of their library
/// face-up and visible to all players at all times while the source permanent
/// is on the battlefield.
///
/// <para>
/// CR 401.5: "Some effects tell a player to play with the top card of their
/// library revealed … If the top card of the player's library changes while a
/// spell is being cast, the new top card won't be revealed and can't be looked
/// at until the spell becomes cast."
/// CR 401.6: "If an effect causes a player to play with the top card of their
/// library revealed, and that particular card stops being revealed for any length
/// of time before being revealed again, it becomes a new object."
/// CR 701.18c: "Some effects instruct a player to 'play' with a certain aspect
/// of the game changed, such as 'Play with the top card of your library
/// revealed.' 'Play' in this sense means to play the Magic game."
/// </para>
///
/// <para>
/// MAST encodes this as a continuous static effect (no <c>Duration</c> stated —
/// the effect persists as long as the source permanent is on the battlefield,
/// per CR 604.2). The imperative "Play with …" form is NOT optional: the
/// controller has no choice. No <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/>
/// wrapper is applied. There are no parameters; the discriminator alone carries
/// the full meaning (subject = You, zone = top of library, visibility = revealed
/// to all). Covers Oracle of Mul Daya and similar library-transparency effects.
/// </para>
/// </summary>
[OracleEffect("playWithTopRevealed")]
public sealed record PlayWithTopRevealedEffect : Effect
{
}
