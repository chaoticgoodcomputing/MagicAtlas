namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] deals damage to an opponent" — any damage (combat or non-combat,
/// Rule 120) specifically to an opponent (not any player, Rule 102.2).
/// Emits <see cref="TriggerEvent.DealsDamageToOpponent"/>. The Filter captures the
/// subject (the thing dealing the damage).
///
/// <para>
/// Distinct from <see cref="DealsCombatDamageConditionRule"/> (combat damage only) and
/// <see cref="DealsDamageConditionRule"/> (any damage, unspecified recipient). The
/// guard excludes "deals combat damage" so combat-damage text routes to
/// <see cref="DealsCombatDamageConditionRule"/> (higher priority), and requires
/// "to an opponent" to distinguish from the generic unrecipient-qualified
/// <see cref="DealsDamageConditionRule"/>.
/// </para>
///
/// <para>
/// Rule 120: a source deals damage when it assigns damage to a player or permanent.
/// Rule 102.2: an opponent is any player not on the same team as the controller.
/// Rule 603.2: triggered abilities fire automatically on matching game events.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 986)]
public sealed class DealsDamageToOpponentConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Must contain "deals damage to an opponent" but NOT "deals combat damage"
    // (that case belongs to DealsCombatDamageConditionRule).
    if (!lower.Contains("deals damage to an opponent"))
    {
      return null;
    }
    if (lower.Contains("deals combat damage"))
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
      Event = TriggerEvent.DealsDamageToOpponent,
      Filter = filter,
    };
  }
}
