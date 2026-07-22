namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "it wasn't put onto the battlefield with this ability" — the anti-recursion
/// loop-prevention gate on Kodama of the East Tree's each-permanent-enters trigger. Kodama
/// lets a permanent that enters put ANOTHER permanent onto the battlefield; this gate stops
/// the put-permanent from re-triggering Kodama in an unbounded chain by excluding any object
/// that itself entered via this very ability.
///
/// <para>
/// A field-less marker: "this ability" is a CR 607-style self-reference to the containing
/// ability, and the polarity is always the negative "wasn't" (the whole predicate is fixed
/// by the card), so there is nothing to parameterise. Mirrors the field-less-idiom
/// convention of <see cref="CastThisObjectCondition"/> / <see cref="VoidCondition"/>.
/// Distinct from every provenance gate that names an EXTERNAL producer — this names the
/// containing ability itself, a self-referential linkage no existing condition expresses.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed anti-recursion gate; the
/// engine reads whether the object entered via this ability, MAST does not pre-evaluate it.
/// Structured rather than left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 603.6 (enters-the-battlefield triggers); CR 609.7 (an ability referring to itself).
/// </summary>
[ConditionKind("notEnteredWithThisAbility")]
public sealed record NotEnteredWithThisAbilityCondition : Condition;
