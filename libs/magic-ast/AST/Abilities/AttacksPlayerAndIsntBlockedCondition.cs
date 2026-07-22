namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this creature attacks a player and isn't blocked" — the compound combat gate on
/// Master of Cruelties' trigger ("Whenever this creature attacks a player and isn't
/// blocked, that player's life total becomes 1. …"). Two conjoined combat facts about
/// the source creature: it is attacking a PLAYER (not a planeswalker or battle, CR
/// 508.1a) and it is UNBLOCKED after the declare-blockers step (CR 509.1h — a creature
/// with no creatures declared to block it becomes an unblocked creature).
///
/// <para>
/// A field-less marker: the subject is always the source creature (the printed subject
/// is "this creature"), the attack target is always "a player" (unparameterised — CR
/// 508.1a distinguishes players from other attackable objects), and "isn't blocked" is a
/// fixed post-declare-blockers state, so the conjunction never varies across the family
/// and there is nothing to parameterise. Neither conjunct is expressible on the existing
/// combat-state axis (<see cref="SourceCombatStateCondition"/> ranges over
/// attacking/blocking of the source, not over the attack TARGET's player-ness nor the
/// blocked/unblocked state), so the compound earns its own arm. Mirrors the field-less
/// idiom convention of <see cref="VoidCondition"/> / <see cref="PairedCondition"/>.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed compound gate; the
/// engine reads whether the source is attacking a player and is unblocked, MAST does not
/// pre-evaluate it. Structured to this dedicated <see cref="Condition"/> arm rather than
/// left as a free-text <see cref="OtherCondition"/> residual — the structured home the
/// <c>AttacksPlayerAndIsntBlockedConditionRule</c> synthesises as its pending
/// intervening-if.
/// </para>
///
/// CR 508.1a (excerpt): "The active player chooses which creatures … will attack. …
/// Each of the chosen creatures … must be attacking a player, planeswalker, or battle."
/// CR 509.1h (excerpt): "A creature that's still in combat and that has no creatures
/// declared as blockers for it becomes an unblocked creature."
/// </summary>
[ConditionKind("attacksPlayerAndIsntBlocked")]
public sealed record AttacksPlayerAndIsntBlockedCondition : Condition;
