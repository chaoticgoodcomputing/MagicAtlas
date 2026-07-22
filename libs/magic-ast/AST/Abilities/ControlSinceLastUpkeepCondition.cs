namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this permanent came under your control since the beginning of your last upkeep"
/// — the intervening-if (CR 603.4) the <em>Echo</em> keyword denotes: true when you
/// gained control of the source permanent at some point within the backward-looking
/// window that opens at the beginning of your most recent previous upkeep and
/// extends to now. A control-provenance / recency gate on the source object: it
/// asks whether control was acquired inside that window (equivalently, that the
/// permanent has NOT been continuously under your control since before your last
/// upkeep began), so a permanent kept from a previous turn fails it and its echo
/// cost is not charged again.
///
/// <para>
/// A field-less marker: this is the fixed, verbatim definition of Echo (CR 702.30a),
/// identical on every Echo card. The subject is always the source permanent
/// (<c>Self</c>) and the window is always "the beginning of your last upkeep", so
/// neither varies and there is nothing to parameterise. Mirrors the field-less-idiom
/// convention used for other fixed condition idioms
/// (<see cref="CastThisObjectCondition"/>, <see cref="PrecedingActionPerformedCondition"/>).
/// Emitted directly by the Echo keyword combinator (<c>EchoKeyword</c>) in place of a
/// free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed control-provenance
/// gate; the engine reads when control of the source permanent was actually acquired
/// relative to the previous upkeep, MAST does not pre-evaluate it.
/// </para>
///
/// CR 702.30a (verbatim): "Echo is a triggered ability. 'Echo [cost]' means 'At the
/// beginning of your upkeep, if this permanent came under your control since the
/// beginning of your last upkeep, sacrifice it unless you pay [cost].'"
/// CR 603.4 (intervening-if): the condition is checked when the ability would
/// trigger and again as it resolves.
/// </summary>
[ConditionKind("controlSinceLastUpkeep")]
public sealed record ControlSinceLastUpkeepCondition : Condition;
