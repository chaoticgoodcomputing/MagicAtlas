namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you had another creature enter the battlefield under your control last turn" — a
/// backward-looking, PREVIOUS-turn entered-the-battlefield gate. Ephara, God of the
/// Polis's each-upkeep intervening-if (CR 603.4): the draw fires only if at least one
/// object matching <see cref="Filter"/> entered the battlefield during the turn BEFORE
/// the current one (CR 514 / the turn structure bounds "last turn" to the immediately
/// preceding turn, distinct from the "this turn" window every existing history predicate
/// uses).
///
/// <para>
/// The counted population is described by <see cref="Filter"/> — Ephara's is
/// <c>{CardTypes:["creature"], Controller:You, ExcludeSelf:true}</c> ("another creature …
/// under your control"; the <see cref="ObjectFilter.ExcludeSelf"/> axis honours the
/// "another", CR 109.5). The gate is an existence check ("had another creature enter" =
/// at least one), so no explicit threshold field is needed.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed last-turn ETB gate; the
/// engine reads whether a matching object entered the battlefield during the previous turn,
/// MAST does not pre-evaluate it. Structured to this dedicated <see cref="Condition"/> arm
/// rather than left as a free-text <see cref="OtherCondition"/> residual. The "last turn"
/// window has no structured history predicate (all existing predicates are "this turn"), so
/// the condition carries it as a dedicated node rather than reusing
/// <see cref="CountCondition"/> over a nonexistent last-turn ETB predicate.
/// </para>
///
/// CR 603.6 (enters-the-battlefield triggers); CR 514 (the turn's steps bound "last turn").
/// </summary>
[ConditionKind("objectEnteredLastTurn")]
public sealed record ObjectEnteredLastTurnCondition : Condition
{
  /// <summary>Which objects, if any entered the battlefield last turn, satisfy the gate — controller/type/self axes.</summary>
  public required ObjectFilter Filter { get; init; }
}
