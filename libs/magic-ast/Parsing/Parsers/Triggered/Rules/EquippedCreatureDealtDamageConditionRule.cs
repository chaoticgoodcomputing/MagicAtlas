namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever equipped creature is dealt damage" — the Equipment-scoped sibling of
/// <see cref="SelfCreatureDealtDamageConditionRule"/> ("this creature is dealt
/// damage") and <see cref="SubtypeDealtDamageConditionRule"/> ("a [Subtype] you
/// control is dealt damage"). The triggering object is the creature the
/// Equipment is currently attached to (CR 301.5 / 702.6), not the Equipment
/// itself and not an arbitrary creature, so the filter carries
/// <see cref="ObjectFilter.IsEquipped"/> — the same axis
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> uses for "equipped
/// creature" elsewhere.
///
/// <para>
/// Blazing Sunsteel: "Whenever equipped creature is dealt damage, it deals that
/// much damage to any target." The effect half is handled separately by
/// <see cref="ItDealsThatMuchDamageToAnyTargetRule"/>, where "it" resolves to
/// <see cref="ObjectReferenceKind.It"/> (the triggering equipped creature, not
/// the Equipment's own <see cref="ObjectReferenceKind.Self"/>).
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players." CR 603.2: "Whenever a game event or game state matches a triggered
/// ability's trigger event, that ability automatically triggers." Priority above
/// the generic <see cref="CreatureDealtDamageConditionRule"/> (500, whose loose
/// "contains creature + is dealt damage" guard would otherwise swallow this
/// clause too and drop the equipped-only restriction), and disjoint from
/// <see cref="SelfCreatureDealtDamageConditionRule"/> (600, requires "this") and
/// <see cref="SubtypeDealtDamageConditionRule"/> (700, requires "you control" +
/// a capitalised subtype) since neither pattern matches the literal word
/// "equipped".
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 650)]
public sealed class EquippedCreatureDealtDamageConditionRule : ITriggerConditionRule
{
  // Anchored on the "equipped creature is dealt damage" surface.
  private static readonly Regex _pattern = new(
    @"\bequipped\s+creature\s+is\s+dealt\s+damage\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(lower))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CreatureDealtDamage,
      Filter = new ObjectFilter { CardTypes = ["creature"], IsEquipped = true },
    };
  }
}
