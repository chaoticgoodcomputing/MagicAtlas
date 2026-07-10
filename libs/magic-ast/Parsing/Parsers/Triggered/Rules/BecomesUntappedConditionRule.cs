namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this artifact becomes untapped" — CR 603.2: "Some trigger events
/// use the word 'becomes' (for example, 'becomes attached' or 'becomes
/// blocked'). These trigger only at the time the named event happens... An
/// ability that triggers when a permanent 'becomes tapped' or 'becomes
/// untapped' doesn't trigger if the permanent enters the battlefield in that
/// state." Sibling of <see cref="BecomesTappedConditionRule"/>, which handles
/// the "becomes tapped" half of the same rules text; this rule targets the
/// <see cref="TriggerEvent.BecomesUntapped"/> event.
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class BecomesUntappedConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes untapped"))
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
      Event = TriggerEvent.BecomesUntapped,
      Filter = filter,
    };
  }
}
