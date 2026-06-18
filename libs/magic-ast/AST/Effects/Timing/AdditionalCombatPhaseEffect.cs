namespace MagicAST.AST.Effects.Timing;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Turn-structure insertion: "After this phase, there is an additional combat phase." —
/// inserts exactly one additional combat phase into the current turn's timeline
/// immediately after the current phase, without an accompanying additional main phase.
///
/// <para>Distinct from <see cref="AdditionalCombatAndMainPhaseEffect"/>, which inserts
/// both a combat phase AND a postcombat main phase (the Aggravated Assault / World at
/// War pattern). Godo, Bandit Warlord and similar cards grant only the combat phase.</para>
///
/// <para>CR 500.1: "A turn consists of five phases in this order: beginning, precombat
/// main, combat, postcombat main, and ending." CR 506 governs the combat phase itself.
/// Effects of this type insert an additional combat phase directly after the current
/// phase as specified by oracle text; the engine determines the resulting postcombat
/// phase classification (CR 505.1a).</para>
/// </summary>
[OracleEffect("additionalCombatPhase")]
public sealed record AdditionalCombatPhaseEffect : Effect
{
}
