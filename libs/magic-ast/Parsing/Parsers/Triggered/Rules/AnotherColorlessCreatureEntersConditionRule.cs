namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another colorless creature you control enters" — trigger condition for watching
/// colorless creature ETB events from the same controller, excluding the source itself.
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers." The "another" qualifier (CR 109) excludes
/// the source permanent from the matching set; the "colorless creature" filter (CR 105.1:
/// "Colorless is not a color") encodes colorlessness via <see cref="ObjectFilter.IsColorless"/>
/// rather than <see cref="ObjectFilter.Colors"/> (which encodes the five named colors).
/// Running at priority 997 to be tried after <see cref="ColorlessSpellCastConditionRule"/>
/// (999) but before the general <see cref="EntersConditionRule"/> (990).
/// </para>
///
/// <para>
/// Example: Glaring Fleshraker — "Whenever another colorless creature you control enters,
/// this creature deals 1 damage to each opponent."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class AnotherColorlessCreatureEntersConditionRule : ITriggerConditionRule
{
  // Matches "another colorless creature you control enters[ the battlefield]"
  // End-anchored; the "the battlefield" suffix is optional (modern oracle omits it).
  // Not anchored at start: the timing word ("Whenever") is still present in triggerText.
  private static readonly Regex _pattern = new(
    @"\banother\s+colorless\s+creature\s+you\s+control\s+enters(?:\s+the\s+battlefield)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("colorless") || !lower.Contains("another"))
    {
      return null;
    }

    if (!lower.Contains("enters"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        IsColorless = true,
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      },
    };
  }
}
