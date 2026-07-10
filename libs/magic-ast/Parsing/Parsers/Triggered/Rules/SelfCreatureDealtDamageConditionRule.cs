namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature is dealt damage" — emits
/// <see cref="TriggerEvent.CreatureDealtDamage"/> with a self-restricted Filter
/// (<c>{CardTypes: ["creature"], IsSelf: true}</c>). This is the "this creature"
/// self sibling of <see cref="CreatureDealtDamageConditionRule"/> (which handles
/// the generic "a creature is dealt damage" — any creature, e.g. Repercussion).
///
/// <para>
/// "This creature" refers to the ability's own source object (CR 109 — the object
/// bearing the ability), so the filter carries <see cref="ObjectFilter.IsSelf"/>
/// to restrict the trigger to that single permanent rather than any creature — the
/// self/any axis the interaction operator gates on (an arbitrary creature is not
/// provably the source). Jackal Pup: "Whenever this creature is dealt damage, it
/// deals that much damage to you."
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players." CR 603.2: "Whenever a game event or game state matches a triggered
/// ability's trigger event, that ability automatically triggers." Higher priority
/// than the generic rule so "this creature" wins the self-restricted reading; the
/// generic rule's guard still catches "a creature" (which lacks "this").
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 600)]
public sealed class SelfCreatureDealtDamageConditionRule : ITriggerConditionRule
{
  // Anchored on the "this creature is dealt damage" surface; the generic sibling
  // (Priority 500) handles "a creature is dealt damage".
  private static readonly Regex _pattern = new(
    @"\bthis\s+creature\s+is\s+dealt\s+damage\b",
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
      Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
    };
  }
}
