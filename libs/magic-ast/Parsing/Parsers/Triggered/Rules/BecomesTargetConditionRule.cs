namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "When this creature becomes the target of a spell or ability" —
/// triggered-ability machinery: Rule 603.1-603.2; "becomes the target" relationship:
/// Rule 115.1. The subject "this creature" is the source permanent; only the
/// self-reference shape is modelled here.
/// </summary>
[TriggerConditionRule(Priority = 983)]
public sealed class BecomesTargetConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes the target"))
    {
      return null;
    }

    // Subject is "this [type]"; delegate to the shared self-reference helper.
    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesTarget,
      Filter = filter,
    };
  }
}
