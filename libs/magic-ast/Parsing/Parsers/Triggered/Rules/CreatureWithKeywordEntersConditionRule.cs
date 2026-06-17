namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a creature you control with [keyword] enters" — an enters trigger on a
/// creature-type filter that additionally requires a keyword ability.
/// Handles Dragon Tempest's first ability: "Whenever a creature you control
/// with flying enters, …"
///
/// Rule 603.6a: enters-the-battlefield abilities trigger when a permanent
/// enters the battlefield. The keyword constraint ("with flying") narrows which
/// entering creatures fire the ability. The constraint is structured on the
/// <see cref="ObjectFilter.Characteristics"/> axis as a
/// <see cref="KeywordCharacteristic"/>, not free text (ADR 0001: no free text
/// for structurable concepts).
///
/// Sits below <see cref="EntersConditionRule"/> (Priority 990) so the plain
/// "a creature you control enters" shape is handled first; this rule fires only
/// when a "with [keyword]" qualifier is present.
/// </summary>
[TriggerConditionRule(Priority = 991)]
public sealed class CreatureWithKeywordEntersConditionRule : ITriggerConditionRule
{
  // Matches "a creature you control with <Keyword> enters"
  // The keyword must be a recognizable MTG keyword ability.
  private static readonly Regex _pattern = new(
    @"\ba\s+creature\s+you\s+control\s+with\s+(?<keyword>[A-Za-z][a-z]+(?:\s+[a-z]+)?)\s+enters",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters") || !lower.Contains("with"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var rawKeyword = m.Groups["keyword"].Value;
    var characteristic = Characteristic.FromLabel(rawKeyword);

    // If the label resolved to an OtherCharacteristic (unrecognised keyword),
    // bail so the fallback can handle it or emit unparsed — no free text.
    if (characteristic is OtherCharacteristic)
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
        Controller = ControllerFilter.You,
        Characteristics = [characteristic],
      },
    };
  }
}
