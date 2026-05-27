namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block evasion (attacker-side): oracle text states that a creature
/// "can't be blocked" (unconditionally) or "can't be blocked by [filter]"
/// (color-restricted). Rule 509.1b (declare-blockers step; evasion abilities
/// constrain the set of legal blocker declarations the defending player can
/// make against the named object).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records
/// that the card's oracle line imposes a "can't be blocked" restriction on
/// the named object; it does not model the runtime decision that the
/// defending player must make at declare-blockers.
///
/// <para>
/// Dual / contrast set under Rule 509:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="CantBeBlockedEffect"/> — attacker-side <i>evasion</i>:
///     the listed creature cannot be declared as blocked by any creature
///     (Rule 509.1b). When <see cref="BlockedByFilter"/> is null, this is
///     full unblockability (Tidal Kraken, Phantom Warrior). When
///     <see cref="BlockedByFilter"/> is set, blocking is prohibited only by
///     creatures matching the filter (Sootwalkers, Vine Mare).
///   </description></item>
///   <item><description>
///     <see cref="MustBeBlockedEffect"/> — attacker-side <i>requirement</i>:
///     the listed creature must be blocked when legally possible.
///   </description></item>
///   <item><description>
///     <see cref="CantBlockEffect"/> — blocker-side <i>restriction</i>:
///     the listed creature cannot be declared as a blocker (Rule 509.1c).
///   </description></item>
///   <item><description>
///     <see cref="MustBlockEffect"/> — blocker-side <i>requirement</i>:
///     the listed creature must block when legally possible (Rule 509.1c).
///   </description></item>
/// </list>
/// <para>
/// Contrast with <see cref="MagicAST.AST.Effects.Keyword.EvasionEffect"/>,
/// which encodes "can't be blocked <i>except by</i> [filter]" — i.e., what
/// CAN block this creature (Flying, Fear, Intimidate shapes). This effect
/// encodes the complementary "can't be blocked <i>by</i> [filter]" —
/// i.e., what CANNOT block it.
/// </para>
/// <para>
/// The subject of the evasion is the static ability's controlling object
/// (the card the ability is printed on). Global lines like
/// "Creatures you control can't be blocked" emit a different shape
/// (mass-affecting effect with a target/filter).
/// </para>
/// </remarks>
[OracleEffect("cantBeBlocked")]
public sealed record CantBeBlockedEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// Filter describing what CANNOT block this creature. When null, blocking
  /// is prohibited unconditionally (full unblockability). When set, only
  /// creatures matching this filter are prohibited from blocking — all other
  /// creatures may still block normally. Rule 509.1b.
  /// e.g., for "can't be blocked by white creatures": Colors = ["W"]
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? BlockedByFilter { get; init; }

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
