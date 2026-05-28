namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature mutates" — mutate trigger (Rule 702.140).
/// Fires each time the creature successfully mutates. The Filter captures the
/// subject ("this creature") as a creature-type filter.
/// </summary>
[TriggerConditionRule(Priority = 980)]
public sealed class MutatesConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("mutates"))
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
      Event = TriggerEvent.Mutates,
      Filter = filter,
    };
  }
}
