namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [CardName] attacks" / "Whenever this creature attacks" /
/// "Whenever a creature you control attacks" — emits <see cref="TriggerEvent.Attacks"/>
/// (Rule 508 — Declare Attackers). The filter shape is shared with dies/enters via
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/>.
/// </summary>
[TriggerConditionRule(Priority = 987)]
public sealed class AttacksConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attacks"))
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
      Event = TriggerEvent.Attacks,
      Filter = filter,
    };
  }
}
