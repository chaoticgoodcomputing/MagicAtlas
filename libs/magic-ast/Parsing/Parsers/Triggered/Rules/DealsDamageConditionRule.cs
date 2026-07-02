namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] deals damage" — any damage (Rule 120), not only combat damage.
/// Emits <see cref="TriggerEvent.DealsDamage"/>. Covers lifelink-analog oracle text
/// printed before the lifelink keyword existed. The combat variant is handled by
/// <see cref="DealsCombatDamageConditionRule"/> (higher priority), so this excludes it.
/// </summary>
[TriggerConditionRule(Priority = 984)]
public sealed class DealsDamageConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Must contain "deals damage" but must NOT contain "deals combat damage".
    if (!lower.Contains("deals damage") || lower.Contains("deals combat damage"))
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
      Event = TriggerEvent.DealsDamage,
      Filter = filter,
    };
  }
}
