namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature becomes tapped" — CR 603.2: "Some trigger events use
/// the word 'becomes' (for example, 'becomes attached' or 'becomes blocked').
/// These trigger only at the time the named event happens... An ability that
/// triggers when a permanent 'becomes tapped' or 'becomes untapped' doesn't
/// trigger if the permanent enters the battlefield in that state." The
/// <see cref="TriggerEvent.BecomesTapped"/> event is distinct from
/// <see cref="TriggerEvent.TapsForMana"/> (which fires specifically on tapping
/// for mana, not any tapping).
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class BecomesTappedConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes tapped"))
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
      Event = TriggerEvent.BecomesTapped,
      Filter = filter,
    };
  }
}
