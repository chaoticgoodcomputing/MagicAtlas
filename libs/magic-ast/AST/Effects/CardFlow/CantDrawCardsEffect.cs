namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A global draw lock — a continuous static effect that stops a player (or
/// players) from drawing cards. e.g. "Players can't draw cards." (Maralen of
/// the Mornsong). A rules-of-the-game-modifying continuous effect (CR 611.1) —
/// written as a plain static statement (CR 604.1: "Static abilities do
/// something all the time rather than being activated or triggered. They are
/// written as statements, and they're simply true.").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces
/// (CR 120, draw). This effect records only who is prohibited from drawing
/// cards (<see cref="Player"/>); it does NOT model the state-based/replacement
/// machinery the rules use to enforce the prohibition — mirroring how
/// <see cref="CantGainLifeEffect"/>'s remarks disclaim engine enforcement of its
/// own lock.
///
/// <para>
/// Keeping <see cref="Player"/> as a field (rather than baking "each player"
/// into the discriminator) lets asymmetric variants — "You can't draw cards",
/// "Your opponents can't draw cards" — reuse this node with a different
/// <see cref="ObjectReference"/> scope. "Players can't draw cards" (the
/// symmetric, all-players case) uses <see cref="ObjectReferenceKind.EachPlayer"/>,
/// mirroring the <see cref="CantGainLifeEffect"/> convention.
/// </para>
/// </remarks>
[OracleEffect("cantDrawCards")]
public sealed record CantDrawCardsEffect : Effect
{
  /// <summary>
  /// Who is prohibited from drawing cards — the scope of the restriction.
  /// "Players can't draw cards" (all players, symmetric) →
  /// <see cref="ObjectReferenceKind.EachPlayer"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
