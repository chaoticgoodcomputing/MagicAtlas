namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "At the beginning of your upkeep" / "...first main phase" / "...draw step" /
/// "...end step" / "...combat on your turn". Maps the phase/step word to a
/// <see cref="TriggerEvent"/>. The "your" / "each opponent's" possessive lands on
/// the filter as a <c>Controller</c>. Only fires for At-timing triggers.
/// </summary>
[TriggerConditionRule(Priority = 1000)]
public sealed class PhaseTriggerConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (timing != TriggerTiming.At)
    {
      return null;
    }

    if (!lower.Contains("beginning of"))
    {
      return null;
    }

    TriggerEvent? evt = null;
    if (lower.Contains("upkeep"))
    {
      evt = TriggerEvent.BeginningOfUpkeep;
    }
    else if (lower.Contains("first main phase") || lower.Contains("precombat main phase"))
    {
      evt = TriggerEvent.BeginningOfPreCombatMainPhase;
    }
    else if (lower.Contains("postcombat main phase") || lower.Contains("second main phase"))
    {
      evt = TriggerEvent.BeginningOfPostCombatMainPhase;
    }
    else if (lower.Contains("draw step"))
    {
      evt = TriggerEvent.BeginningOfDrawStep;
    }
    else if (lower.Contains("end step"))
    {
      evt = TriggerEvent.BeginningOfEndStep;
    }
    else if (lower.Contains("combat"))
    {
      evt = TriggerEvent.BeginningOfCombat;
    }

    if (evt is null)
    {
      return null;
    }

    // Possessive cue determines the filter's controller axis. "your" → You,
    // "each opponent's" → Opponent, "each player's" → no filter (universal).
    ObjectFilter? filter = null;
    if (lower.Contains("your"))
    {
      filter = new ObjectFilter { Controller = ControllerFilter.You };
    }
    else if (lower.Contains("each opponent"))
    {
      filter = new ObjectFilter { Controller = ControllerFilter.Opponent };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = evt.Value,
      Filter = filter,
    };
  }
}
