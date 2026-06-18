namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Compound Eerie trigger: "Whenever an enchantment you control enters and whenever
/// you fully unlock a Room" (CR 207.2c ability word — Eerie). This is a single
/// triggered ability that fires on EITHER of two events:
/// (1) whenever an enchantment you control enters the battlefield, or
/// (2) whenever you fully unlock a Room permanent.
///
/// <para>
/// CR 603.2: a triggered ability triggers whenever any of its listed events occurs.
/// The "and whenever" phrase in oracle text combines two disjoint trigger conditions
/// into one ability; the ability fires whenever EITHER condition is met.
/// The primary condition (enchantment enters) is encoded on <see cref="TriggerCondition"/>
/// (Trigger); the secondary condition (fully unlock a Room) is encoded on
/// <see cref="TriggeredAbility.AdditionalTrigger"/>.
/// </para>
///
/// <para>
/// CR 709.5i: "Some abilities trigger when a player 'fully unlocks' a permanent
/// with a shared type line. Such an ability triggers when that permanent has one
/// of the two unlocked designations and gets the other, or when it has neither
/// designation and gains both." The Filter on the secondary trigger carries
/// Controller = You (the ability fires only when the controller fully unlocks a Room).
/// </para>
///
/// <para>
/// Eerie is an ability word (CR 207.2c) — it has no special rules meaning and is
/// stripped by TriggeredAbilityParser before trigger-condition parsing. This rule
/// must be tried at higher priority than the generic <see cref="EntersConditionRule"/>
/// so the compound phrase is matched before the enchantment-enters sub-phrase.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class EnchantmentEntersOrFullyUnlocksRoomConditionRule : ITriggerConditionRule
{
  // The full compound trigger phrase (after ability-word prefix is stripped):
  // "Whenever an enchantment you control enters and whenever you fully unlock a Room"
  private static readonly Regex _pattern = new(
    @"^whenever\s+an\s+enchantment\s+you\s+control\s+enters\s+and\s+whenever\s+you\s+fully\s+unlock\s+a\s+Room$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The secondary trigger condition: "whenever you fully unlock a Room."
  /// CR 709.5i: fires when the second unlocked designation is assigned to a Room
  /// permanent the ability controller controls.
  /// </summary>
  private static readonly TriggerCondition _secondaryTrigger = new()
  {
    Timing = TriggerTiming.Whenever,
    Event = TriggerEvent.FullyUnlockRoom,
    Filter = new ObjectFilter
    {
      CardTypes = ["enchantment"],
      Subtypes = ["Room"],
      Controller = ControllerFilter.You,
    },
  };

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick pre-filter before running the full regex.
    if (!lower.Contains("enchantment") || !lower.Contains("fully unlock") || !lower.Contains("room"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    // Signal the secondary trigger via the thread-static piggyback pattern
    // (same mechanism as EntersAndOpponentDrawsNotFirstConditionRule).
    PendingAdditionalTrigger = _secondaryTrigger;

    // Primary trigger: "Whenever an enchantment you control enters"
    return new TriggerCondition
    {
      Timing = TriggerTiming.Whenever,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["enchantment"],
        Controller = ControllerFilter.You,
      },
    };
  }

  /// <summary>
  /// The additional trigger condition synthesized by the most recent successful
  /// <see cref="Match"/> call on this thread. TriggeredAbilityParser reads this
  /// immediately after calling <see cref="Match"/> and resets it to null to prevent
  /// stale reads on the next call.
  /// </summary>
  [ThreadStatic]
  public static TriggerCondition? PendingAdditionalTrigger;
}
