namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if it targets a blue spell" — a condition that checks whether the subject
/// (typically "it", the spell or ability containing this condition) has, among its
/// chosen targets, an object matching the stated filter. The structured form of
/// Mystical Dispute's conditional self-cost-reduction: "This spell costs {2} less
/// to cast if it targets a blue spell." CR 118.7: a cost may be reduced by an
/// amount contingent on a stated condition; here the condition is evaluated
/// against the target(s) already chosen for this spell as it's cast (CR 601.2c —
/// targets are chosen before costs are determined, CR 601.2f).
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed gate — the
/// subject and the filter its target must match — the engine reads the actual
/// chosen target's characteristics and does not have this pre-evaluated. Distinct
/// from <see cref="ObjectHasSubtypeCondition"/>/<see cref="ObjectHasCardTypeCondition"/>
/// (which check a characteristic of the subject object itself): this checks a
/// characteristic of an object the subject TARGETS, composing the existing
/// <see cref="ObjectFilter"/> primitive (its <c>Colors</c>/<c>CardTypes</c> axes)
/// rather than a bespoke color field.
/// </para>
///
/// <para>
/// <see cref="Subject"/> mirrors the pronoun-string convention of
/// <see cref="ObjectHasSubtypeCondition.Subject"/>/<see cref="ObjectHasCardTypeCondition.Subject"/>
/// — typically <c>"It"</c> (the spell/ability containing this condition, referring
/// to itself).
/// </para>
/// </summary>
[ConditionKind("targetsFilter")]
public sealed record TargetsFilterCondition : Condition
{
  /// <summary>
  /// The pronoun identifying the subject whose target is being checked —
  /// typically <c>"It"</c> (this spell, referring to itself).
  /// </summary>
  public required string Subject { get; init; }

  /// <summary>The filter the subject's target must match — e.g. a blue spell.</summary>
  public required ObjectFilter Filter { get; init; }
}
