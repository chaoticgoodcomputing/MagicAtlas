namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// The honest logical AND of two or more independent board-state predicates —
/// "If an opponent controls a Forest <b>and</b> you control a Swamp, …"
/// (Deepwood Legate). Every sub-condition in <see cref="Conditions"/> must hold for
/// the compound to hold. Composes the existing <see cref="Condition"/> primitives
/// (each conjunct is typically a <see cref="CountCondition"/>) rather than flattening
/// the phrase into a single free-text <see cref="OtherCondition"/> residual, which
/// would under-structure the gate and hide the two controller axes (opponent's
/// Forest vs. your Swamp).
///
/// <para>
/// Needed because <see cref="StaticAbility.Condition"/> and
/// <see cref="MagicAST.AST.Effects.ConditionalEffect"/> each carry a single
/// <see cref="Condition"/>, but an "X and Y" gate requires both predicates to be true
/// simultaneously. Modelling the two conjuncts as two separate static abilities would
/// read as an OR (either one applying independently) — semantically wrong. This arm
/// keeps the conjunction as one node so the AND is faithful (ADR 0007, composing
/// primitives over new leaves).
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the conjunction as written; the
/// engine evaluates each conjunct against game state, MAST does not pre-evaluate it.
/// </para>
/// </summary>
[ConditionKind("all")]
public sealed record AllCondition : Condition
{
  /// <summary>
  /// The conjuncts — every one must hold for the compound condition to hold.
  /// Each is an independent board-state predicate (e.g. two
  /// <see cref="CountCondition"/>s with different controller axes).
  /// </summary>
  public required IReadOnlyList<Condition> Conditions { get; init; }
}
