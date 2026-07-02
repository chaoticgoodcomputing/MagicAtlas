namespace MagicAST.AST.Effects.Timing;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Turn-structure insertion: "After this main phase, there is an additional combat phase
/// followed by an additional main phase." — inserts a combat phase and a postcombat main
/// phase into the current turn's timeline immediately after the current main phase.
///
/// <para>CR 500.1: "A turn consists of five phases in this order: beginning, precombat main,
/// combat, postcombat main, and ending." Effects of this type insert an additional combat
/// and postcombat-main sequence into the current turn (CR 500.7: "Some spells and abilities
/// can add phases to a player's turn. They do this by adding the phases directly after the
/// specified phase.").</para>
///
/// <para>
/// MAST describes what the text says. The exact insertion point ("after this main phase")
/// and the sequence of inserted phases are the full printed content of this effect.
/// Whether the inserted phases are a "precombat" or "postcombat" main from the turn's
/// perspective is an engine determination (CR 505.1a); MAST faithfully records the printed
/// structure without baking in that distinction.
/// </para>
/// </summary>
[OracleEffect("additionalCombatAndMainPhase")]
public sealed record AdditionalCombatAndMainPhaseEffect : Effect
{
}
