namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "no mana was spent to cast it" — a cast-history gate that holds when the total mana
/// spent to cast the referenced spell was zero (Boromir, Warden of the Tower: "Whenever
/// an opponent casts a spell, if no mana was spent to cast it, counter that spell.").
/// Catches spells cast for an alternative cost of {0} or for free (CR 118.5: "If a cost
/// is reduced to nothing … it is considered to be a cost of {0}.").
///
/// <para>
/// A field-less marker: "no mana was spent to cast it" is a fixed, parameter-free idiom
/// — the amount is always zero and the subject is always the triggering spell ("it").
/// The amount-zero total sits alongside the color-keyed
/// <see cref="ManaSpentToCastCondition"/> ("{G} was spent to cast it", a per-color boolean)
/// but reads the aggregate NUMBER of mana spent rather than a single color, and the fixed
/// zero threshold leaves nothing to parameterise (a nonzero threshold would be a
/// <see cref="QuantityComparisonCondition"/> over
/// <see cref="MagicAST.AST.Quantities.ManaSpentToCastQuantity"/>, not this marker).
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): the engine reads the actual total mana spent to
/// cast the triggering spell (CR 601.2f–h); MAST does not pre-evaluate it. Structured to
/// this dedicated <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 601.2h (excerpt): "The player pays the total cost. … Partial payments are not
/// allowed. Unpayable costs can't be paid." — the total mana spent is a fact fixed once
/// the spell finishes being cast.
/// CR 118.5 (verbatim): "If a cost is reduced to nothing by a cost-reduction effect, it is
/// considered to be a cost of {0}."
/// </summary>
[ConditionKind("noManaSpentToCast")]
public sealed record NoManaSpentToCastCondition : Condition;
