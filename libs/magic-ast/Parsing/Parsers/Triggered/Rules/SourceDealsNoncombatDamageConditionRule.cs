namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a source you control deals noncombat damage to an opponent" —
/// noncombat-damage trigger gated to sources the controller owns
/// (CR 609.7: a source is any object, spell, or ability that deals damage;
/// CR 510.1: combat damage is dealt only in the Combat Damage Step).
/// Emits <see cref="TriggerEvent.NoncombatDamageDealt"/>.
///
/// <para>
/// The filter records <c>CardTypes = ["source"]</c> with <c>Controller = You</c>:
/// "source" is the CR 609.7 rules term for any damage-dealing entity that a
/// player controls, modelled as a CardTypes singleton so it is structurally
/// parallel to "a creature you control" and "a permanent you control" filters.
/// </para>
///
/// <para>
/// Runs at priority 986 — above <see cref="DealsCombatDamageConditionRule"/> (985)
/// and <see cref="DealsDamageConditionRule"/> (984) — so "deals noncombat damage"
/// is claimed before those rules consume the more-general "deals damage" surface.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 986)]
public sealed class SourceDealsNoncombatDamageConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"\ba\s+source\s+you\s+control\s+deals\s+noncombat\s+damage\s+to\s+an\s+opponent\b",
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
      Event = TriggerEvent.NoncombatDamageDealt,
      Filter = new ObjectFilter
      {
        CardTypes = ["source"],
        Controller = ControllerFilter.You,
      },
    };
  }
}
