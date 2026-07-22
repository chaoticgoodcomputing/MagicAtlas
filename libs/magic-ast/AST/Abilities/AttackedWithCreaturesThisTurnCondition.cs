namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you attacked with three or more creatures this turn" — a COUNT-thresholded attack
/// history gate. Windbrisk Heights's activated-ability condition on playing the
/// hideaway-exiled card: the permission applies only if the controller declared at least
/// <see cref="Count"/> creatures as attackers this turn.
///
/// <para>
/// The count-bearing sibling of the field-less <see cref="YouAttackedThisTurnCondition"/>
/// (the Raid yes/no "you attacked this turn"): Raid only asks WHETHER you attacked, this
/// asks with HOW MANY, so the threshold cannot be folded into that marker.
/// <see cref="Count"/> is the attacker-count comparison — Windbrisk's is
/// <c>{GreaterThanOrEqual, 3}</c>. The subject is always the controller ("you"), so it is
/// not parameterised.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed attacker-count gate; the
/// engine reads how many creatures you declared as attackers this turn, MAST does not
/// pre-evaluate it. Structured rather than left as part of a free-text residual.
/// </para>
///
/// CR 508.1 (declaring attackers); CR 514 ("this turn" bounds the window).
/// </summary>
[ConditionKind("attackedWithCreaturesThisTurn")]
public sealed record AttackedWithCreaturesThisTurnCondition : Condition
{
  /// <summary>The attacker-count threshold — Windbrisk's is <c>{GreaterThanOrEqual, 3}</c>.</summary>
  public required Comparison Count { get; init; }
}
