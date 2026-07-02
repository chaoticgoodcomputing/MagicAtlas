namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature blocks" / "Whenever [CardName] blocks" /
/// "Whenever this creature blocks a creature" — Rule 509 (Declare Blockers Step).
/// Excludes "becomes blocked" (BecomesBlocked event), which also contains "blocks".
/// </summary>
[TriggerConditionRule(Priority = 986)]
public sealed class BlocksConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("blocks") || lower.Contains("becomes blocked"))
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
      Event = TriggerEvent.Blocks,
      Filter = filter,
    };
  }
}
