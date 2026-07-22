namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you attacked this turn" — the Raid gate: a backward-looking, turn-scoped check on whether
/// YOU declared an attacker (CR 508.1) during the current turn. Raid is an ability word (CR
/// 207.2c — no rules meaning of its own) whose printed condition is exactly "if you attacked
/// this turn"; the enters-with-a-counter clause it gates (e.g. the Raid printing of Abzan
/// Skycaptain: "This creature enters with a +1/+1 counter on it if you attacked this turn")
/// applies only when the controller declared at least one attacker earlier this turn.
///
/// <para>
/// A field-less marker: the subject is always the controller ("you") and the event is always
/// "attacked this turn" (a creature you controlled was declared as an attacker, CR 508.1), so
/// there is nothing to parameterise. Mirrors the field-less-idiom convention used for other
/// fixed condition idioms (<see cref="VoidCondition"/>,
/// <see cref="ControlSinceLastUpkeepCondition"/>). Distinct from the
/// <see cref="MagicAST.AST.References.AttackedThisTurnPredicate"/> HistoryPredicate, which
/// restricts an <see cref="ObjectFilter"/> to the creatures that attacked ("each creature that
/// attacked this turn"): THIS is the player-scoped yes/no gate — did the controller attack at
/// all — in <see cref="Condition"/> position. Reference-not-resolution (ADR 0004): MAST records
/// the printed gate; the engine reads whether you declared an attacker this turn, MAST does not
/// pre-evaluate it. Structured rather than left as a free-text <see cref="OtherCondition"/>
/// residual.
/// </para>
///
/// CR 508.1 (excerpt): "First, the active player declares attackers. … To declare attackers,
/// the active player follows the steps below, in order."
/// CR 207.2c (excerpt): ability words "have no special rules meaning and no individual entries
/// in the Comprehensive Rules." — "raid" is among them.
/// </summary>
[ConditionKind("youAttackedThisTurn")]
public sealed record YouAttackedThisTurnCondition : Condition;
