namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this Vehicle becomes crewed [for the first time each turn]" — Rule 702.122 trigger.
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class BecomesCrewedConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes crewed"))
    {
      return null;
    }

    // Filter expresses the subject. "this Vehicle" is the common shape.
    ObjectFilter? filter = null;
    if (lower.Contains("this vehicle"))
    {
      filter = new ObjectFilter { Characteristics = [Characteristic.Other("this Vehicle")] };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesCrewed,
      Filter = filter,
    };
  }
}
