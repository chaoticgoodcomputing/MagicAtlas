namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block restriction (blocker-side): oracle text states that a creature
/// "can block only [filter]." Rule 509.1c (declare-blockers step; blocking
/// restrictions constrain the set of legal blocker declarations the defending
/// player can make).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line narrows the set of legal blockees to those matching
/// <see cref="Filter"/>; it does not model the runtime decision the defending
/// player must make at declare-blockers.
///
/// <para>
/// Distinct from <see cref="CantBlockEffect"/> (Rule 509.1c, blanket
/// can't-block — no filter, the creature simply can't be declared as a
/// blocker) and from <see cref="MustBlockEffect"/> (Rule 509.1c, blocker-side
/// requirement — the listed creature must be declared as a blocker when it
/// legally can). This is a <i>narrowing</i> restriction: the creature CAN
/// block, but only against attackers matching the filter (typically
/// "creatures with flying", paralleling Reach as a soft anti-evasion answer).
/// </para>
/// <para>
/// The subject of the restriction is the static ability's controlling object
/// (the card the ability is printed on). For global lines like "All creatures
/// can block only creatures with flying" the parser would emit a different
/// shape with an explicit subject.
/// </para>
/// </remarks>
[OracleEffect("canBlockOnly")]
public sealed record CanBlockOnlyEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The filter narrowing the set of legal blockees. Most commonly
  /// <c>{ CardTypes: ["creature"], Characteristics: ["with flying"] }</c>
  /// for the standard ground-can-block-only-fliers shape.
  /// </summary>
  public required ObjectFilter Filter { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
