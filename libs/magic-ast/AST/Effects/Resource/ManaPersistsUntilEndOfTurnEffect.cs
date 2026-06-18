namespace MagicAST.AST.Effects.Resource;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Until end of turn, you don't lose this mana as steps and phases end."
///
/// <para>
/// A continuous effect (CR 611) that modifies the turn-based mana-emptying action
/// (CR 106.4 / CR 500.5: "Each player's mana pool empties at the end of each step
/// and phase") — the mana produced by this ability persists until the end of turn
/// rather than being lost at each step/phase boundary.
/// </para>
///
/// <para>
/// CR 106.4 (verbatim): "When an effect instructs a player to add mana, that mana
/// goes into a player's mana pool. From there, it can be used to pay costs
/// immediately, or it can stay in the player's mana pool as unspent mana. Each
/// player's mana pool empties at the end of each step and phase, and the player is
/// said to lose this mana."
/// </para>
///
/// <para>
/// CR 500.5 (verbatim): "As a step or phase ends, if there are effects that last
/// until the end of that step or phase, those effects expire. Then any unspent mana
/// left in a player's mana pool empties."
/// </para>
///
/// <para>
/// The effect is produced alongside an <see cref="AddManaEffect"/> in a triggered
/// ability context — it extends the lifetime of that triggered mana to end of turn.
/// The <see cref="ContinuousEffect.Duration"/> is always
/// <see cref="UntilTimeDuration.EndOfTurn"/> for this oracle clause.
/// </para>
/// </summary>
[OracleEffect("manaPersists")]
public sealed record ManaPersistsUntilEndOfTurnEffect : ContinuousEffect;
