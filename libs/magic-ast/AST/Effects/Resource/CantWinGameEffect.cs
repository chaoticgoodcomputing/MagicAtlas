namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Your opponents can't win the game." (Herald of Eternal Dawn) — a blanket static
/// lock that stops the scoped player(s) from winning the game by any means (CR
/// 104.2a: "A player still in the game wins the game if that player's opponents
/// have all left the game. This happens immediately and overrides all effects that
/// would preclude that player from winning the game." — acknowledging that such
/// preclusion effects exist and scoping their reach). Written as a plain static
/// statement (CR 604.1: "Static abilities do something all the time rather than
/// being activated or triggered. They are written as statements, and they're
/// simply true.").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not the SBA/priority machinery that
/// enforces it. This effect records only who is prohibited from winning the game
/// (<see cref="Player"/>); it does not model win-check evaluation.
///
/// <para>
/// Distinct from <see cref="CantLoseGameEffect"/>: that node exempts the controller
/// from loss conditions (CR 104.3), while this node locks the named opponents out
/// of win conditions (CR 104.2) — the two effects are frequently paired on the same
/// card ("You can't lose the game and your opponents can't win the game.") but are
/// separate rules concepts and separate nodes.
/// </para>
/// </remarks>
[OracleEffect("cantWinGame")]
public sealed record CantWinGameEffect : Effect
{
  /// <summary>
  /// Who is prohibited from winning the game. Herald of Eternal Dawn reads "your
  /// opponents can't win the game" so this is
  /// <see cref="ObjectReferenceKind.EachOpponent"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
