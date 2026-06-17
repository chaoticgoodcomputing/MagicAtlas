namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Compound trigger: "When this creature enters and whenever an opponent draws a card
/// except the first one they draw in each of their draw steps" — Orcish Bowmasters
/// (LTR). This is a single triggered ability that fires on EITHER of two events:
/// (1) when this creature enters the battlefield, or (2) whenever an opponent draws
/// any card that is not the first card drawn in their draw step.
///
/// <para>
/// CR 603.2: a triggered ability triggers whenever any of its listed events occurs.
/// The "and" in oracle text ("When A and whenever B") combines two disjoint trigger
/// conditions into one ability; the ability fires whenever EITHER condition is met.
/// The primary condition (ETB) is encoded on <see cref="TriggerCondition"/> (Trigger);
/// the secondary condition (opponent-draws-not-first) is encoded on
/// <see cref="TriggeredAbility.AdditionalTrigger"/>. Both share the same effect list.
/// </para>
///
/// <para>
/// The "except the first one they draw in each of their draw steps" qualifier is
/// captured on the secondary condition's <see cref="TriggerCondition.ExceptFirstDrawStep"/>
/// flag (CR 121.1: "a player draws a card"; CR 603.2: the qualifier narrows which
/// draw events match). This is descriptive — the engine determines whether a given
/// draw event is the first draw of the step; MAST records that the exclusion exists.
/// </para>
///
/// <para>
/// Must be tried at higher priority than the generic Enters and OpponentDrawsCard
/// rules so the full compound phrase is matched before either sub-phrase.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 996)]
public sealed class EntersAndOpponentDrawsNotFirstConditionRule : ITriggerConditionRule
{
  // The full compound trigger phrase — "When this creature enters and whenever an
  // opponent draws a card except the first one they draw in each of their draw steps"
  private static readonly Regex _pattern = new(
    @"^when\s+this\s+creature\s+enters\s+and\s+whenever\s+an\s+opponent\s+draws\s+a\s+card\s+except\s+the\s+first\s+one\s+they\s+draw\s+in\s+each\s+of\s+their\s+draw\s+steps$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The secondary trigger condition node synthesized from the "and whenever an opponent
  /// draws a card except the first one they draw in each of their draw steps" branch.
  /// Stored statically since it is invariant for this rule family.
  /// </summary>
  private static readonly TriggerCondition _secondaryTrigger = new()
  {
    Timing = TriggerTiming.Whenever,
    Event = TriggerEvent.DrawsCard,
    Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
    ExceptFirstDrawStep = true,
  };

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick pre-filter before running the full regex.
    if (!lower.Contains("enters") || !lower.Contains("draws") || !lower.Contains("draw steps"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    // The primary trigger is the ETB condition ("When this creature enters").
    // The caller (TriggeredAbilityParser) sets Trigger = this return value.
    // The secondary (opponent-draws) condition is signalled by attaching it as
    // AdditionalTrigger; we attach it on the returned TriggerCondition directly.
    // NOTE: TriggerCondition does not carry AdditionalTrigger — that field lives on
    // TriggeredAbility. We synthesize the full ability via a custom approach: return
    // the ETB TriggerCondition here and expose the secondary through a companion
    // property so TriggeredAbilityParser can read it.
    //
    // Implementation note: because ITriggerConditionRule.Match can only return one
    // TriggerCondition, we piggyback the secondary on a thread-local so the caller
    // can retrieve it immediately after. The alternative — modifying the interface —
    // would require touching infra files.
    PendingAdditionalTrigger = _secondaryTrigger;

    return new TriggerCondition
    {
      Timing = TriggerTiming.When,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
    };
  }

  /// <summary>
  /// The additional trigger condition synthesized by the most recent successful
  /// <see cref="Match"/> call on this thread. TriggeredAbilityParser reads this
  /// immediately after calling <see cref="Match"/> (same synchronous call chain,
  /// same thread) and resets it to null to prevent stale reads on the next call.
  ///
  /// <para>
  /// Thread-local because the parser may be called concurrently from multiple
  /// test threads. The get/reset pattern (read then null) is safe within a single
  /// synchronous parse call.
  /// </para>
  /// </summary>
  [ThreadStatic]
  public static TriggerCondition? PendingAdditionalTrigger;
}
