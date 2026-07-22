namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Compound trigger condition: "this creature attacks a player and isn't blocked" —
/// fires when the creature attacks and the compound condition holds. Produces a plain
/// <see cref="TriggerEvent.Attacks"/> trigger on self; the compound qualifier
/// "attacks a player and isn't blocked" is communicated as a pending
/// <see cref="PendingInterveningIf"/> for <c>TriggeredAbilityParser</c> to pick up.
///
/// <para>
/// Rule 508.1: "The active player declares attackers" — attack target is a player
/// (not a planeswalker or battle). Rule 509 (declare-blockers step): "isn't blocked"
/// is the state immediately after blockers are declared. Together they form the
/// compound condition governing when this trigger fires.
/// </para>
///
/// <para>
/// Must be tried at higher priority (Priority = 990) than
/// <see cref="AttacksConditionRule"/> (Priority = 987) so this specific compound
/// form is matched before the generic "attacks" rule consumes the text and drops
/// the "a player and isn't blocked" qualifier.
/// The pattern is anchored (^...$) so it cannot silently match as a substring.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class AttacksPlayerAndIsntBlockedConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^(?:Whenever\s+)?this\s+(?:creature|permanent)\s+attacks\s+a\s+player\s+and\s+isn'?t\s+blocked$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attacks") || !lower.Contains("player") || !lower.Contains("blocked"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    // The compound "attacks a player and isn't blocked" qualifier is not directly
    // expressible on TriggerCondition (which has no AttackTarget or Unblocked field).
    // Communicate it as a pending InterveningIf via the thread-local below, mirroring
    // the PendingAdditionalTrigger approach used by EntersAndOpponentDrawsNotFirstConditionRule.
    // Structured to the dedicated AttacksPlayerAndIsntBlockedCondition marker (CR
    // 508.1a / 509.1h) rather than an OtherCondition free-text residual.
    PendingInterveningIf = new AttacksPlayerAndIsntBlockedCondition();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
    };
  }

  /// <summary>
  /// The intervening-if condition synthesized by the most recent successful
  /// <see cref="Match"/> call on this thread. <c>TriggeredAbilityParser</c>
  /// reads this immediately after calling <see cref="Match"/> (same synchronous
  /// call chain, same thread) and resets it to <c>null</c> to prevent stale reads.
  ///
  /// <para>
  /// Thread-local so concurrent test threads don't share state. Mirrors the
  /// <c>PendingAdditionalTrigger</c> pattern on
  /// <see cref="EntersAndOpponentDrawsNotFirstConditionRule"/>.
  /// </para>
  /// </summary>
  [ThreadStatic]
  public static Condition? PendingInterveningIf;
}
