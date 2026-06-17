namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if [object] dies this way" — a causation-gate condition that is true when the
/// referenced object died <i>as a direct result of this ability's own preceding
/// destroy effect</i> (CR 701.7a: "To destroy a permanent, move it to its owner's
/// graveyard"). Distinct from an arbitrary death-trigger condition
/// (<see cref="MagicAST.AST.Triggers.TriggerEvent.Dies"/>): this condition is
/// <i>effect-scoped</i>, not trigger-scoped — it gates the follow-on effect within
/// the same spell/ability resolution, checking whether indestructible (CR 702.12) or
/// a replacement effect prevented the move-to-graveyard that the destroy effect
/// would otherwise cause.
///
/// <para>
/// "Its controller creates two tokens that are copies of that creature, except …"
/// (Saw in Half, CLB) is the canonical example: the copy-token effect fires only if
/// the creature was not indestructible and no replacement suppressed the death.
/// MAST describes the condition as written; the engine evaluates it at resolution
/// (ADR 0004 — reference-not-resolution).
/// </para>
///
/// <para>
/// <see cref="Reference"/> names the object being checked (typically
/// <see cref="ObjectReferenceKind.It"/> for "that creature" — the immediately
/// preceding destroy target).
/// </para>
/// </summary>
[ConditionKind("diedThisWay")]
public sealed record DiedThisWayCondition : Condition
{
  /// <summary>
  /// The object whose death-as-a-result-of-this-effect is being checked —
  /// typically <c>{Kind:"It"}</c> ("that creature" from the preceding destroy).
  /// </summary>
  public required ObjectReference Reference { get; init; }
}
