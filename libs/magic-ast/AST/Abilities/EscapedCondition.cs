namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This creature escapes with [counters] on it" — a marker condition gating a
/// replacement effect on whether the source permanent "escaped" (CR 702.138b,
/// verbatim): "A spell or permanent \"escaped\" if that spell or the spell that
/// became that permanent as it resolved was cast from a graveyard with an escape
/// ability."
///
/// <para>
/// CR 702.138c (verbatim) reduces the "escapes with [counters]" template to this
/// exact conditional-entry shape: "An ability that reads \"[This permanent]
/// escapes with [one or more of a kind of counter]\" means \"If this permanent
/// escaped, it enters with [those counters]\" ..." — i.e. the counter-granting
/// replacement (<see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/> under
/// a <see cref="StaticAbility"/> with <see cref="StaticAbility.When"/> =
/// <see cref="StaticTimingKind.AsThisEnters"/>) only applies when this condition
/// holds.
/// </para>
///
/// <para>
/// A marker (no fields): the subject is always the source permanent itself
/// (Self) — the only form this condition takes. Reference-not-resolution (ADR
/// 0004): the engine reads whether the permanent escaped; MAST does not
/// pre-evaluate it.
/// </para>
/// </summary>
[ConditionKind("escaped")]
public sealed record EscapedCondition : Condition;
