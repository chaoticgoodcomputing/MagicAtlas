namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another [color] creature you control enters" — color-filtered creature ETB trigger,
/// excluding the source permanent (the "another" qualifier).
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers." The "another" qualifier (CR 109.5) excludes
/// the source permanent from the matching set; the color filter narrows which entering
/// creatures match (e.g. "another green creature you control enters" — only green creatures
/// controlled by the trigger's controller, not the source itself).
/// </para>
///
/// <para>
/// Uses <see cref="ObjectFilter.Colors"/> (not <see cref="ObjectFilter.IsColorless"/>) per
/// Rule 105.1: "Colorless is not a color" — the five named MTG colors (white, blue, black,
/// red, green) are encoded as single-letter codes (W, U, B, R, G) in the Colors list.
/// </para>
///
/// <para>
/// Running at priority 996 — tried after <see cref="AnotherColorlessCreatureEntersConditionRule"/>
/// (997) and before the general <see cref="EntersConditionRule"/> (990).
/// </para>
///
/// <para>
/// Example: Ivy Lane Denizen — "Whenever another green creature you control enters,
/// put a +1/+1 counter on target creature."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 996)]
public sealed class AnotherColorCreatureEntersConditionRule : ITriggerConditionRule
{
  // Matches "another <color> creature you control enters[[ the battlefield]]"
  // End-anchored; the "the battlefield" suffix is optional (modern oracle omits it).
  // Not anchored at start: the timing word ("Whenever") is still present in triggerText.
  private static readonly Regex _pattern = new(
    @"\banother\s+(?<color>white|blue|black|red|green)\s+creature\s+you\s+control\s+enters(?:\s+the\s+battlefield)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _colorCodes = new(StringComparer.OrdinalIgnoreCase)
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("another") || !lower.Contains("creature") || !lower.Contains("enters"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var colorName = m.Groups["color"].Value;
    if (!_colorCodes.TryGetValue(colorName, out var colorCode))
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
        Colors = [colorCode],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      },
    };
  }
}
