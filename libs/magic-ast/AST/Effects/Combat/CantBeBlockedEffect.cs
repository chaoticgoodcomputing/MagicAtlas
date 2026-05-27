namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block evasion (attacker-side): oracle text states that a creature
/// "can't be blocked" (unconditionally), "can't be blocked by [filter]"
/// (color-/power-restricted), or "can't be blocked by more than one creature"
/// (blocker-count restriction). Rule 509.1b (declare-blockers step; evasion
/// abilities constrain the set of legal blocker declarations the defending
/// player can make against the named object).
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
///     (Rule 509.1b). When <see cref="BlockedByFilter"/> is null and
///     <see cref="MaxBlockers"/> is null, this is full unblockability
///     (Tidal Kraken, Phantom Warrior). When <see cref="BlockedByFilter"/>
///     is set, blocking is prohibited only by creatures matching the filter
///     (Sootwalkers, Vine Mare). When <see cref="MaxBlockers"/> is set,
///     blocking is capped at that count (Stalking Tiger: MaxBlockers = 1).
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
/// When <see cref="Target"/> is null, the restriction applies to the static
/// ability's controlling object (the card the ability is printed on),
/// e.g. "This creature can't be blocked by more than one creature." When
/// set, it names a distinct object, e.g. <c>EnchantedOrEquipped</c> for
/// Aura bodies such as "Enchanted creature can't be blocked by more than
/// one creature." Mirrors <see cref="CantBlockEffect.Target"/>.
/// </para>
/// </remarks>
[OracleEffect("cantBeBlocked")]
public sealed record CantBeBlockedEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The object the evasion applies to. Null means the static ability's
  /// controlling object (the printed card itself); set for Aura/Equipment
  /// bodies such as "Enchanted creature can't be blocked by more than one
  /// creature." Mirrors <see cref="CantBlockEffect.Target"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// Filter describing what CANNOT block this creature. When null, blocking
  /// is prohibited unconditionally (full unblockability). When set, only
  /// creatures matching this filter are prohibited from blocking — all other
  /// creatures may still block normally. Rule 509.1b.
  /// e.g., for "can't be blocked by white creatures": Colors = ["W"]
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? BlockedByFilter { get; init; }

  /// <summary>
  /// Maximum number of creatures that may be declared as blockers for this
  /// creature. When null, no blocker-count restriction applies (the filter
  /// axis handles WHAT can block; this axis handles HOW MANY can block).
  /// When set to 1, records the oracle clause "can't be blocked by more than
  /// one creature" (Stalking Tiger pattern, Rule 509.1b). The value is always
  /// a positive integer representing the maximum legal blocker count.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MaxBlockers { get; init; }

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
