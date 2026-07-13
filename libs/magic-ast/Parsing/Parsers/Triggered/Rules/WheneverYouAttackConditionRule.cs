namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you attack" (bare, player-scoped attack-declaration trigger) — Guide of
/// Souls: "Whenever you attack, you may pay {E}{E}{E}. …". Emits
/// <see cref="TriggerEvent.Attacks"/> with a creature-you-control filter.
///
/// <para>
/// CR 508.3d: "An ability that reads 'Whenever [a player] attacks, . . .' triggers if one
/// or more creatures that player controls are declared as attackers." The filter's
/// Controller = You + CardTypes = ["creature"] captures the "creatures you control"
/// attacker set (CR 508.1a — the active player chooses which creatures they control
/// attack), matching the shape used by the count-gated sibling
/// <see cref="AttackWithNumberOrMoreCreaturesConditionRule"/> (minus its MinimumCount).
/// </para>
///
/// <para>
/// ANCHORED (^…$): matches ONLY the bare "you attack" form (nothing after "attack"), so it
/// cannot shadow the qualified siblings "you attack with two or more creatures"
/// (<see cref="AttackWithNumberOrMoreCreaturesConditionRule"/>) or "you attack with [Name]
/// and another creature" (<see cref="AttackWithAndAnotherConditionRule"/>). The optional
/// "Whenever" prefix is tolerated because <c>SplitTriggerAndEffect</c> leaves the timing
/// word on the trigger half (mirrors those siblings). Bare "you attack" does not contain
/// the substring "attacks", so it never reaches <see cref="AttacksConditionRule"/>.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 500)]
public sealed class WheneverYouAttackConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^\s*(?:Whenever\s+)?you\s+attack\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(triggerText))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
    };
  }
}
