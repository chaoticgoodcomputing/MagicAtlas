namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "dies" triggers: "this creature dies", "a creature dies", "another creature dies", etc.
/// "dies" is modern oracle; the longform "is put into a graveyard from the battlefield"
/// (Rule 700.4) describes the same event and normalises to the same TriggerEvent.Dies.
/// </summary>
[TriggerConditionRule(Priority = 991)]
public sealed class DiesConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("dies") && !lower.Contains("is put into a graveyard from the battlefield"))
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
      Event = TriggerEvent.Dies,
      Filter = filter,
    };
  }
}
