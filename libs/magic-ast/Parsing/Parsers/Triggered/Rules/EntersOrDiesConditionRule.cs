namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "enters or dies" compound triggers — fires on either zone-change event.
/// Rule 603: "When [subject] enters or dies, [effect]." The oracle phrase
/// "enters or dies" (always in that order) denotes a single triggered ability
/// watching for the permanent either entering the battlefield or dying. Must be
/// tried before the individual Enters/Dies rules (higher priority) so the
/// disjunction isn't split and misclassified.
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class EntersOrDiesConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters or dies"))
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
      Event = TriggerEvent.EntersOrDies,
      Filter = filter,
    };
  }
}
