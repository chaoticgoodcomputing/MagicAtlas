namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-attack restriction (attacker-side): oracle text states that a
/// creature "can't attack." Rule 509.1d (declare-attackers step; attacking
/// restrictions constrain the set of legal attacker declarations the active
/// player can make).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line imposes a "can't attack" restriction on the named
/// object; it does not model the runtime decision that the active player must
/// make at declare-attackers.
///
/// <para>
/// This is the dual of <see cref="MustAttackEffect"/> — same rule (508/509.1d
/// boundary), opposite polarity:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustAttackEffect"/> is an attacker-side <i>requirement</i>:
///     the listed creature must be declared as an attacker when it legally
///     can.
///   </description></item>
///   <item><description>
///     <see cref="CantAttackEffect"/> is an attacker-side <i>restriction</i>:
///     the listed creature is excluded from the set of legal attacker
///     declarations.
///   </description></item>
/// </list>
/// <para>
/// Directly parallels <see cref="CantBlockEffect"/> (Rule 509.1c) on the
/// blocker side. The two are often paired in a single oracle clause, e.g.
/// "Enchanted creature can't attack or block." Per the multi-effect-per-clause
/// doctrine that clause yields two <see cref="Effect"/> records on one
/// <c>StaticAbility</c> — a <see cref="CantAttackEffect"/> and a
/// <see cref="CantBlockEffect"/> — not a combined node.
/// </para>
/// <para>
/// When <see cref="Target"/> is null, the restriction applies to the static
/// ability's controlling object (the card the ability is printed on),
/// e.g. "This creature can't attack." When set, it names a distinct object,
/// e.g. <c>EnchantedOrEquipped</c> for the Aura body
/// "Enchanted creature can't attack."
/// </para>
/// </remarks>
[OracleEffect("cantAttack")]
public sealed record CantAttackEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The object the restriction applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies and global restrictions.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

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
