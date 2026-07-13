namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[this permanent] has a [counter type] counter on it" — a board-state predicate
/// that is true while the named object currently has one or more counters of the
/// given kind. The structured form of the recurring Impending / time-counter gate
/// ("as long as … it has a time counter on it" / "if … it has a time counter on it").
///
/// <para>
/// Distinct from <see cref="TriggeringObjectCounterCondition"/> ("triggeringObjectCounter"),
/// which is a <i>look-back</i> predicate on the object a leaves-the-battlefield trigger
/// refers to (CR 603.10, dies-triggers): this condition is a <i>present-tense</i> read of
/// the counters on a live, named <see cref="Subject"/> (typically
/// <see cref="ObjectReferenceKind.Self"/> for "it has a time counter on it").
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the predicate as written; the engine
/// reads the subject's current counters, MAST does not pre-evaluate it. Composes with
/// <see cref="AllCondition"/> for the compound Impending gates ("its impending cost was
/// paid AND it has a time counter on it").
/// </para>
/// </summary>
[ConditionKind("objectHasCounter")]
public sealed record ObjectHasCounterCondition : Condition
{
  /// <summary>
  /// The permanent whose counters are read — typically
  /// <see cref="ObjectReferenceKind.Self"/> ("it has a time counter on it").
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The counter kind the condition checks (e.g. <c>"time"</c>, <c>"+1/+1"</c>).
  /// True when the subject has at least one counter of this kind.
  /// </summary>
  public required string CounterType { get; init; }
}
