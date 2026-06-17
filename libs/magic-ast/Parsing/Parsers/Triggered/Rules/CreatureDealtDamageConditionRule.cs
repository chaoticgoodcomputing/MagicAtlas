namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a creature is dealt damage" — emits
/// <see cref="TriggerEvent.CreatureDealtDamage"/> with a Filter of
/// <c>{CardTypes: ["creature"]}</c> (Rule 120 — Damage; Rule 603.2 — triggered
/// abilities). Covers the Repercussion trigger pattern: any creature (not just a
/// specific one) receiving damage fires the ability.
///
/// <para>
/// CR 120.1: "Any time an object, player, or battle would be dealt damage, that
/// damage is dealt instead to the appropriate target." CR 120.3: "Damage can be
/// dealt to creatures, planeswalkers, battles, and players." CR 603.2: "Whenever a
/// game event or game state matches a triggered ability's trigger event, that
/// ability automatically triggers." The filter carries the creature predicate so
/// the trigger's subject is clearly a creature (not a player or planeswalker).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 500)]
public sealed class CreatureDealtDamageConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Guard: must contain "creature" and "is dealt damage" (passive damage-receipt).
    // Distinct from the active "deals damage" family (DealsDamageConditionRule).
    if (!lower.Contains("creature") || !lower.Contains("is dealt damage"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CreatureDealtDamage,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };
  }
}
