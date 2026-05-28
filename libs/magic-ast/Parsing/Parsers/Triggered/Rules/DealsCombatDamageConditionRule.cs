namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] deals combat damage to (a player|an opponent|any player)" —
/// emits <see cref="TriggerEvent.DealsCombatDamageToPlayer"/> (Rule 510 — Combat
/// Damage Step; Rule 603.6 — triggered abilities). The recipient class is implied
/// by the enum value; the Filter captures the subject (what is dealing the damage).
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class DealsCombatDamageConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("deals combat damage"))
    {
      return null;
    }

    // Require a player-class recipient: "to a player", "to an opponent", "to any player".
    if (
      !lower.Contains("to a player")
      && !lower.Contains("to an opponent")
      && !lower.Contains("to any player")
    )
    {
      return null;
    }

    // Subject is the thing doing the dealing: "this creature", self-by-name, etc.
    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToPlayer,
      Filter = filter,
    };
  }
}
