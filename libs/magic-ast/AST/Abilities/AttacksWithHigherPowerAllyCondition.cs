namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this creature attacks with at least one other creature with greater power" — the
/// Training gate (CR 702.149a; Hopeful Initiate). The +1/+1 counter is put only when the
/// source is declared as an attacker in the same combat as at least one OTHER attacking
/// creature whose power is greater than the source's power.
///
/// <para>
/// A field-less marker: the subject is always the source ("this creature"), the co-attacker
/// is always "at least one other" with strictly "greater power" than the source, and the
/// combat is always the current one — the whole predicate is fixed by CR 702.149a, so there
/// is nothing to parameterise. Mirrors the field-less-idiom convention of
/// <see cref="YouAttackedThisTurnCondition"/> / <see cref="VoidCondition"/>. The combat
/// cohort relation ("attacks WITH", i.e. declared together this combat) combined with the
/// relative-power comparison is expressible on no <see cref="MagicAST.AST.References.ObjectFilter"/>
/// axis, so it is a dedicated node rather than a <see cref="CountCondition"/>.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed Training gate; the engine
/// reads the attacking cohort and their powers, MAST does not pre-evaluate it. Structured
/// rather than left as a free-text <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 702.149a (Training); CR 508.1 (declaring attackers).
/// </summary>
[ConditionKind("attacksWithHigherPowerAlly")]
public sealed record AttacksWithHigherPowerAllyCondition : Condition;
