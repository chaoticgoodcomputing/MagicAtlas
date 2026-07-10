namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You can't lose the game." (Herald of Eternal Dawn, Platinum Angel) — a blanket
/// static lock that exempts the scoped player from every way to lose the game (CR
/// 104.3: "There are several ways to lose the game."), not just the 0-or-less-life
/// state-based loss condition. Written as a plain static statement (CR 604.1:
/// "Static abilities do something all the time rather than being activated or
/// triggered. They are written as statements, and they're simply true.").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not the SBA-checking machinery that
/// enforces it. This effect records only who is exempted from losing the game
/// (<see cref="Player"/>); it does not model state-based-action evaluation.
///
/// <para>
/// Distinct from <see cref="CantLoseGameForZeroLifeEffect"/>, which narrowly scopes
/// its exemption to the 0-or-less-life loss condition specifically (CR 704.5a). This
/// node is the blanket form referenced by that node's own remarks (e.g. Platinum
/// Angel), covering every way a player can lose (CR 104.3a-k).
/// </para>
/// </remarks>
[OracleEffect("cantLoseGame")]
public sealed record CantLoseGameEffect : Effect
{
  /// <summary>
  /// Who is exempted from losing the game. Herald of Eternal Dawn reads "You can't
  /// lose the game …" so this is <see cref="ObjectReferenceKind.You"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
