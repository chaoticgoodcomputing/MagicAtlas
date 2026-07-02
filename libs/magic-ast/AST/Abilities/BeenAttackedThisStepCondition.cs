namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you've been attacked this step" — the intervening casting-time gate on the
/// combat-trick family (Warrior's Stand, Defiant Stand, Harsh Justice): true iff a
/// creature was declared attacking you during the CURRENT Declare Attackers Step
/// (CR 508.1a/508.1b — the active player chooses the attackers and announces which
/// player each attacks; Glossary "Attacking Creature").
///
/// <para>
/// A fieldless marker (mirroring <see cref="PrecedingActionPerformedCondition"/> and
/// the layout of <see cref="SourceCombatStateCondition"/>): "you" is the spell's
/// controller and "this step" is the current Declare Attackers Step — both are
/// inherent to the idiom, so there is nothing to parameterise. Structuring it keeps
/// the phrase out of the <see cref="OtherCondition"/> free-text residual.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed gate; the engine
/// evaluates it against the actual combat state (whether a creature was declared
/// attacking this player during the Declare Attackers Step), MAST does not
/// pre-evaluate it to a boolean.
/// </para>
///
/// Glossary "Attacking Creature" (verbatim): "A creature that has either been
/// declared as part of a legal attack during the combat phase (once all costs to
/// attack, if any, have been paid), or a creature that has been put onto the
/// battlefield attacking. It remains an attacking creature until it's removed from
/// combat or the combat phase ends, whichever comes first. See rule 508, 'Declare
/// Attackers Step.'"
/// CR 508.1a (verbatim): "The active player chooses which creatures that they
/// control, if any, will attack. The chosen creatures must be untapped, they can't
/// also be battles, and each one must either have haste or have been controlled by
/// the active player continuously since the turn began."
/// CR 508.1b (verbatim): "If the defending player controls any planeswalkers, is the
/// protector of any battles, or the game allows the active player to attack multiple
/// other players, the active player announces which player, planeswalker, or battle
/// each of the chosen creatures is attacking."
/// </summary>
[ConditionKind("beenAttackedThisStep")]
public sealed record BeenAttackedThisStepCondition : Condition;
