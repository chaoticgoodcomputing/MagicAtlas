namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A global life-gain lock — a continuous static effect that stops a player (or
/// players) from gaining life. e.g. "Players can't gain life." (Giant Cindermaw).
/// This is a rules-of-the-game-modifying continuous effect (CR 611.1: a continuous
/// effect "affects players or the rules of the game, for a fixed or indefinite
/// period"), written as a plain static statement (CR 604.1: "Static abilities do
/// something all the time rather than being activated or triggered. They are
/// written as statements, and they're simply true.").
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// CR 119.7: "If an effect says that a player can't gain life, that player can't
/// make an exchange such that the player's life total would become higher; in
/// that case, the exchange won't happen. Similarly, if an effect redistributes
/// life totals, a player can't receive a new life total such that the player's
/// life total would become higher. In addition, a cost that involves having that
/// player gain life can't be paid, and a replacement effect that would replace a
/// life gain event affecting that player won't do anything." This effect records
/// only who is prohibited from gaining life (<see cref="Player"/>); it does NOT
/// model the SBA/replacement-effect machinery that CR 119.7 describes as the
/// enforcement mechanism — mirroring how <see cref="CantCastMoreThanNSpellsEffect"/>'s
/// remarks disclaim engine enforcement of its cap.
///
/// <para>
/// Baseline CR 119.3: "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly." — this effect is the
/// prohibition that overrides that baseline adjustment for the scoped player(s).
/// </para>
///
/// <para>
/// Keeping <see cref="Player"/> as a field (rather than baking "each player" into
/// the discriminator) lets asymmetric variants — "You can't gain life", "Your
/// opponents can't gain life" — reuse this node with a different
/// <see cref="ObjectReference"/> scope. "Players can't gain life" (the symmetric,
/// all-players case) uses <see cref="ObjectReferenceKind.EachPlayer"/>.
/// </para>
/// </remarks>
[OracleEffect("cantGainLife")]
public sealed record CantGainLifeEffect : Effect
{
  /// <summary>
  /// Who is prohibited from gaining life — the scope of the restriction.
  /// "Players can't gain life" (all players, symmetric) →
  /// <see cref="ObjectReferenceKind.EachPlayer"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
