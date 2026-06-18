namespace MagicAST.AST.Effects.Timing;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Turn-structure insertion: "After this phase, there is an additional combat phase."
/// Inserts a single combat phase into the current turn's timeline immediately after
/// the current phase — without an accompanying additional main phase.
///
/// <para>
/// CR 500.8 (verbatim): "Some effects can add phases to a turn. They do this by
/// adding the phases directly after the specified phase. If multiple extra phases
/// are created after the same phase, the most recently created phase will occur
/// first."
/// </para>
///
/// <para>
/// Distinct from <see cref="AdditionalCombatAndMainPhaseEffect"/> ("After this
/// <em>main</em> phase, there is an additional combat phase <em>followed by an
/// additional main phase</em>"). Combat Celebrant inserts only a combat phase
/// after the current phase (typically the declare-attackers step of an existing
/// combat phase), with no accompanying additional main phase.
/// </para>
///
/// <para>
/// MAST records what the oracle text states ("after this phase, there is an
/// additional combat phase"). The specific insertion mechanics (which phase becomes
/// the postcombat main, etc.) are engine territory (CR 505.1a, 505.1b).
/// </para>
/// </summary>
[OracleEffect("additionalCombatPhase")]
public sealed record AdditionalCombatPhaseEffect : Effect
{
}
