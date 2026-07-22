namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "if it was a creature" — a LAST-KNOWN-INFORMATION card-type predicate on the triggering
/// object of a leaves-the-battlefield trigger: the object HAD the stated card type immediately
/// before the event (CR 603.10a — a dies/leaves trigger looks back in time at the object's
/// last-known information; CR 608.2h). Enduring Tenacity: "When Enduring Tenacity dies, if it
/// was a creature, return it to the battlefield …" — the enchantment-creature may have already
/// stopped being a creature (a prior effect), so the gate reads its type as it last existed on
/// the battlefield.
///
/// <para>
/// The past-tense look-back sibling of the present-tense <see cref="ObjectHasCardTypeCondition"/>
/// ("as long as enchanted permanent IS a creature"), exactly as
/// <see cref="TriggeringObjectCounterCondition"/> ("it had a +1/+1 counter") is the look-back
/// sibling of the present-tense <see cref="ObjectHasCounterCondition"/>: the tense distinction is
/// a real CR 603.10 distinction (last-known information vs current characteristics), not vocabulary
/// sprawl. MAST records the predicate as written — reference-not-resolution (ADR 0004): the engine
/// reads the dying object's last-known type line; MAST does not pre-evaluate it.
/// </para>
/// </summary>
[ConditionKind("triggeringObjectType")]
public sealed record TriggeringObjectTypeCondition : Condition
{
  /// <summary>
  /// The card type the triggering object is checked to have HAD — e.g. <c>"creature"</c>
  /// (CR 205.2a, lowercase to match the
  /// <see cref="MagicAST.AST.References.ObjectFilter.CardTypes"/> vocabulary).
  /// </summary>
  public required string CardType { get; init; }

  /// <summary>
  /// True for "it was a [type]" (had the type); false for "it wasn't a [type]" (did not).
  /// Carries the polarity, mirroring <see cref="TriggeringObjectCounterCondition.Present"/>.
  /// </summary>
  public required bool Present { get; init; }
}
