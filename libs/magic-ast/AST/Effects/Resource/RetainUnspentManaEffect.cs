namespace MagicAST.AST.Effects.Resource;

using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You don't lose unspent [color] mana as steps and phases end." — a permanent
/// continuous effect (CR 611) that scopes the turn-based mana-emptying action
/// (CR 500.4 / CR 106.4) to exempt unspent mana of a single named color: that
/// color's mana stays in the controller's mana pool across step/phase
/// boundaries instead of being lost.
///
/// <para>
/// CR 500.4 (verbatim): "As a step or phase begins, if there are effects that
/// last until that step or phase, those effects expire."
/// </para>
///
/// <para>
/// CR 106.4 (verbatim): "When an effect instructs a player to add mana, that
/// mana goes into a player's mana pool. From there, it can be used to pay costs
/// immediately, or it can stay in the player's mana pool as unspent mana. Each
/// player's mana pool empties at the end of each step and phase, and the player
/// is said to lose this mana."
/// </para>
///
/// <para>
/// Distinct from <see cref="ManaPersistsUntilEndOfTurnEffect"/>: that node is a
/// one-time, end-of-turn-scoped extension paired with a specific
/// <see cref="AddManaEffect"/> inside a single triggered ability's resolution
/// (Firebending's "add N {R}. Until end of combat, you don't lose this mana as
/// steps and phases end."). This node instead is a stand-alone, always-on
/// static ability (Ashling, Flame Dancer) that is not tied to any one
/// mana-producing effect or triggered ability, is not scoped to a single turn,
/// and applies to ALL unspent mana of the named <see cref="Color"/> the
/// controller holds at any step/phase boundary — so it carries no
/// <see cref="ContinuousEffect.Duration"/> (it persists for as long as the
/// permanent remains on the battlefield, CR 604.2) and a required
/// <see cref="Color"/> rather than an implicit "this mana" back-reference.
/// </para>
///
/// <para>
/// MAST is descriptive: this node records the oracle declaration that the named
/// color's mana is exempt from the emptying action. The mana-pool bookkeeping
/// itself (which symbols are present, when they are spent) is engine territory
/// (ADR 0003/0004 describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("retainUnspentMana")]
public sealed record RetainUnspentManaEffect : ContinuousEffect
{
  /// <summary>
  /// The single color of unspent mana that is exempted from emptying (CR 105.1:
  /// white, blue, black, red, green). Oracle printings of this static ability
  /// name exactly one color.
  /// </summary>
  public required ManaColor Color { get; init; }
}
