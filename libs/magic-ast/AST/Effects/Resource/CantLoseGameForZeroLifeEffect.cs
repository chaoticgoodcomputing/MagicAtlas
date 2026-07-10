namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You don't lose the game for having 0 or less life." (Lich's Tomb) — a static
/// replacement that overrides the zero-or-less-life state-based loss condition
/// (CR 704.5a: "A player with 0 or less life loses the game.") for the scoped
/// player. Written as a plain static statement (CR 604.1: "Static abilities do
/// something all the time rather than being activated or triggered. They are
/// written as statements, and they're simply true.").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not the SBA-checking machinery that
/// enforces it. This effect records only who is exempted from the 0-or-less-life
/// loss condition (<see cref="Player"/>); it does not model state-based-action
/// evaluation, mirroring how <see cref="CantGainLifeEffect"/>'s remarks disclaim
/// engine enforcement of CR 119.7. Narrowly scoped to the 0-or-less-life loss
/// condition specifically — distinct from a blanket "can't lose the game" grant
/// (e.g. Platinum Angel), which would exempt a player from every loss condition,
/// not just CR 704.5a.
/// </remarks>
[OracleEffect("cantLoseGameForZeroLife")]
public sealed record CantLoseGameForZeroLifeEffect : Effect
{
  /// <summary>
  /// Who is exempted from the 0-or-less-life loss condition. Lich's Tomb reads
  /// "You don't lose the game …" so this is <see cref="ObjectReferenceKind.You"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
