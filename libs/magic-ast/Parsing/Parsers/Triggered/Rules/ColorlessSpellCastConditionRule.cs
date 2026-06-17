namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "you cast a colorless spell" — trigger condition for watching colorless spell casts.
///
/// Distinct from the general <see cref="SpellCastConditionRule"/> (priority 998) which
/// handles color-word qualifiers (white, blue, black, red, green) but not "colorless".
/// Colorless is not a color (Rule 105.1: "Colorless is not a color."), so it is encoded
/// via <see cref="ObjectFilter.IsColorless"/> rather than the <see cref="ObjectFilter.Colors"/>
/// axis. Running at priority 999 ensures this rule is tried before the general rule.
///
/// Examples: Forsaken Monument — "Whenever you cast a colorless spell, you gain 2 life."
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class ColorlessSpellCastConditionRule : ITriggerConditionRule
{
  // Non-start-anchored so it matches even when the timing word ("Whenever") is still
  // in triggerText. End-anchored to avoid matching longer clauses.
  private static readonly Regex _pattern = new(
    @"\byou\s+cast\s+a\s+colorless\s+spell\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("colorless") || !lower.Contains("cast"))
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
      Event = TriggerEvent.SpellCast,
      Filter = new ObjectFilter
      {
        CardTypes = ["spell"],
        IsColorless = true,
        Controller = ControllerFilter.You,
      },
    };
  }
}
