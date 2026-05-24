namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-attack restriction: oracle text states that a creature "attacks each combat if able."
/// Rule 508.1d (declared attackers must include any creature with such a requirement when possible).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's oracle line
/// imposes an "attacks each combat if able" requirement on the named object; it does not model
/// the runtime decision that the player must make at declare-attackers.
///
/// Typical Target is the card itself (an <c>ObjectReference</c> with kind <c>Self</c>),
/// but the same shape covers lines like "Goblins you control attack each combat if able"
/// where the target is an <c>ObjectFilter</c>-shaped reference.
/// </remarks>
[OracleEffect("mustAttack")]
public sealed record MustAttackEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The object the restriction applies to. Usually <c>Self</c> for a creature whose
  /// own oracle line says "[Self] attacks each combat if able."
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
