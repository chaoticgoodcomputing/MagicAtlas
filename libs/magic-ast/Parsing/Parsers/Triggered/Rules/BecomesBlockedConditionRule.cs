namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature becomes blocked" — Rule 509 (Declare Blockers Step).
/// Fires on the attacking creature when one or more blockers are assigned to it.
/// The <see cref="TriggerEvent.BecomesBlocked"/> event is distinct from
/// <see cref="TriggerEvent.Blocks"/> (the blocking creature's trigger).
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class BecomesBlockedConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes blocked"))
    {
      return null;
    }

    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesBlocked,
      Filter = filter,
    };
  }
}
