namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you attack with two or more creatures, [effect]" — an attacker-count-gated
/// attack-declaration trigger (Military Intelligence). Emits <see cref="TriggerEvent.Attacks"/>
/// with <see cref="TriggerCondition.MinimumCount"/> set to the named threshold and a
/// creature-you-control filter.
///
/// <para>
/// CR 508.3c: "An ability that reads "Whenever [a player] attacks with [a creature], . . ."
/// triggers if a creature that player controls is declared as an attacker."
/// </para>
/// <para>
/// CR 508.3d: "An ability that reads "Whenever [a player] attacks, . . ." triggers if one or
/// more creatures that player controls are declared as attackers." (supports the "two or more"
/// count framing on top of the base attack-declaration event.)
/// </para>
/// <para>
/// CR 508.1a: "The active player chooses which creatures that they control, if any, will
/// attack. ..." — justifies the Filter's Controller = You.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 989)]
public sealed class AttackWithNumberOrMoreCreaturesConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attack") || !lower.Contains("or more"))
    {
      return null;
    }

    // Pattern: "Whenever you attack with <count> or more creatures".
    // The optional "Whenever" prefix is required because SplitTriggerAndEffect leaves the
    // timing word on the trigger half (mirrors AttackWithAndAnotherConditionRule).
    var match = Regex.Match(
      triggerText,
      @"^\s*(?:Whenever\s+)?you\s+attack\s+with\s+(?<count>two|three|four|five|six|seven|eight|nine|ten|\d+)\s+or\s+more\s+creatures\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value.ToLowerInvariant();
    int? count = countText switch
    {
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.TryParse(countText, out var n) ? n : null,
    };

    if (count == null)
    {
      // Unrecognised count word — bail so the fallback path records the gap.
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      MinimumCount = count,
      Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
    };
  }
}
