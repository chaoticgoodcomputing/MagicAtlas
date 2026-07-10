namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever one or more creatures you control deal combat damage to a player" —
/// the aggregate combat-damage payoff family (Rapacious Guest, Professional
/// Face-Breaker, Excogitator Sphinx, Vincent, Vengeful Atoner, and many other
/// cards sharing this exact subject phrase with varying payoffs). The plural
/// "one or more creatures" subject takes the plural verb "deal" (not "deals"),
/// so <see cref="DealsCombatDamageConditionRule"/>'s "deals combat damage" guard
/// never matches this shape — this is a dedicated sibling rule, not an extension
/// of the shared file, so it cannot collide with that rule's singular-subject
/// forms (CardName/this creature/a creature you control with [keyword]).
///
/// <para>
/// Emits <see cref="TriggerEvent.DealsCombatDamageToPlayer"/> with a generic
/// creature-you-control filter (no subtype restriction, unlike
/// <see cref="OneOrMoreSubtypeDealsDamageToOpponentsConditionRule"/>'s named-subtype
/// shape) and <see cref="TriggerCondition.MinimumCount"/> = 1, mirroring the
/// "one or more Foods" quantity qualifier already used for
/// <see cref="TriggerEvent.Sacrifices"/> (Camellia, the Seedmiser).
/// </para>
///
/// <para>
/// ANCHORED (^…$) against the full trigger clause (after the timing-word strip)
/// so this rule matches ONLY the exact "…to a player" recipient form — the
/// sibling "…to one or more players" (Forth Eorlingas!, The Destined Thief) and
/// "…to a player this turn" (Jace, Cunning Castaway) surfaces deliberately fall
/// through unmatched rather than being coerced into this shape.
/// </para>
///
/// <para>
/// Rule 510.1 (Combat Damage Step); Rule 603.2: "Whenever a game event or game
/// state matches a triggered ability's trigger event, that ability automatically
/// triggers."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 986)]
public sealed class OneOrMoreCreaturesDealCombatDamageToPlayerConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^one\s+or\s+more\s+creatures\s+you\s+control\s+deal\s+combat\s+damage\s+to\s+a\s+player$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("one or more creatures") || !lower.Contains("deal combat damage"))
    {
      return null;
    }

    var body = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );

    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToPlayer,
      Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      MinimumCount = 1,
    };
  }
}
