namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] deals combat damage to a creature" — emits
/// <see cref="TriggerEvent.DealsCombatDamageToCreature"/>.
///
/// <para>
/// CR 510.1 (Combat Damage Step): "First, the active player announces how each
/// attacking creature assigns its combat damage, then the defending player
/// announces how each blocking creature assigns its combat damage. This
/// turn-based action doesn't use the stack." The combat-damage assignment is the
/// game event; CR 603.2 — "Whenever a game event or game state matches a
/// triggered ability's trigger event, that ability automatically triggers."
/// </para>
///
/// <para>
/// The recipient class (a creature) is implied by the enum value; the Filter
/// captures the subject — the thing dealing the damage ("this creature",
/// self-by-name, etc.). Sits above <see cref="DealsCombatDamageConditionRule"/>
/// (player recipient, Priority 985) and <see cref="DealsDamageConditionRule"/>
/// (any damage, Priority 984) so the creature-recipient variant is recognised
/// before the broader shapes; those sibling rules guard on "to a player" /
/// exclude "deals combat damage" respectively, so there is no overlap.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 986)]
public sealed class DealsCombatDamageToCreatureConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("deals combat damage"))
    {
      return null;
    }

    // Require a creature-class recipient: "to a creature" / "to another creature".
    if (!lower.Contains("to a creature") && !lower.Contains("to another creature"))
    {
      return null;
    }

    // Subject is the thing doing the dealing. "this creature deals combat damage
    // to a creature" resolves to the self-reference filter (ParseObjectFilter
    // matches "this creature" before the trailing "a creature" recipient).
    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToCreature,
      Filter = filter,
    };
  }
}
