namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block restriction (blocker-side): oracle text states that a creature
/// "can't block." Rule 509.1c (declare-blockers step; blocking restrictions
/// constrain the set of legal blocker declarations the defending player can
/// make).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line imposes a "can't block" restriction on the named
/// object; it does not model the runtime decision that the defending player
/// must make at declare-blockers.
///
/// <para>
/// This is the dual of <see cref="MustBlockEffect"/> — same rule (509.1c),
/// opposite polarity:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustBlockEffect"/> is a blocker-side <i>requirement</i>:
///     the listed creature must be declared as a blocker when it legally can.
///   </description></item>
///   <item><description>
///     <see cref="CantBlockEffect"/> is a blocker-side <i>restriction</i>:
///     the listed creature is excluded from the set of legal blocker
///     declarations.
///   </description></item>
/// </list>
/// <para>
/// Distinct from <see cref="MustBeBlockedEffect"/>, which is an attacker-side
/// requirement under the same rule. Distinct from
/// <c>DefenderEffect</c> (Rule 702.3) — that's the formal keyword ability
/// (printed as "Defender") which also imposes a can't-attack restriction.
/// "This creature can't block." is the sentence form on cards that aren't
/// formally Defenders.
/// </para>
/// <para>
/// No parameters — this is a descriptive marker. The subject of the
/// restriction is the static ability's controlling object (the card the
/// ability is printed on); for global lines like "All creatures can't block"
/// the parser would emit a different shape.
/// </para>
/// </remarks>
[OracleEffect("cantBlock")]
public sealed record CantBlockEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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
