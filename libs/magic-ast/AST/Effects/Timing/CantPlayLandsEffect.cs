namespace MagicAST.AST.Effects.Timing;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Player] can't play lands." — a static restriction that prohibits the scoped
/// player from playing land cards (Aggressive Mining: "You can't play lands.").
///
/// CR 305.1: "A player who has priority may play a land card from their hand
/// during a main phase of their turn when the stack is empty. Playing a land is
/// a special action; it doesn't use the stack." CR 116.2a: "Playing a land is a
/// special action ... By default, a player can take this action only once during
/// each of their turns." This effect overrides that default permission entirely
/// for the named player — no land plays are legal for that player while the
/// effect is active — a rules-of-the-game-modifying continuous effect (CR 611.1),
/// written as a plain static statement (CR 604.1: "Static abilities do something
/// all the time rather than being activated or triggered. They are written as
/// statements, and they're simply true.").
/// </summary>
/// <remarks>
/// Keeping <see cref="Player"/> as a field (rather than baking "you" into the
/// discriminator) lets asymmetric and symmetric variants — "You can't play
/// lands", "Players can't play lands", "Your opponents can't play lands" — reuse
/// this node with a different <see cref="ObjectReference"/> scope, mirroring the
/// <see cref="MagicAST.AST.Effects.CardFlow.CantDrawCardsEffect"/> /
/// <see cref="MagicAST.AST.Effects.Resource.CantGainLifeEffect"/> convention.
/// </remarks>
[OracleEffect("cantPlayLands")]
public sealed record CantPlayLandsEffect : Effect
{
  /// <summary>
  /// Who is prohibited from playing lands — the scope of the restriction.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
