namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you attack with [Name] and another [qualifier] creature" (Merry shape) —
/// models the companion-attack trigger (Rule 508). Emits <see cref="TriggerEvent.Attacks"/>
/// with a companion-creature filter carrying the "another" characteristic plus any
/// supertype qualifier.
/// </summary>
[TriggerConditionRule(Priority = 988)]
public sealed class AttackWithAndAnotherConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attack") || !lower.Contains("another"))
    {
      return null;
    }

    // Pattern: "Whenever you attack with [Name] and another [adj] creature".
    // The [adj] group captures optional qualifiers like "legendary" before "creature".
    var match = Regex.Match(
      triggerText,
      @"^\s*(?:Whenever\s+)?you\s+attack\s+with\s+\S.*?\s+and\s+another\s+(?<adj>[\w\s]+?)\s+creature\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var adjText = match.Groups["adj"].Value.Trim().ToLowerInvariant();

    // Build companion-creature filter. "legendary" maps to Supertypes;
    // unrecognised qualifiers fall through to null (bail).
    List<string>? supertypes = null;
    List<string>? characteristics = null;

    if (!string.IsNullOrWhiteSpace(adjText))
    {
      var knownSupertypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        "legendary",
        "basic",
        "snow",
        "world",
      };
      if (knownSupertypes.Contains(adjText))
      {
        supertypes = [adjText.Substring(0, 1).ToUpperInvariant() + adjText.Substring(1).ToLowerInvariant()];
      }
      else
      {
        // Unrecognised qualifier — bail so the fallback path records the gap.
        return null;
      }
    }

    // "another" is a characteristic exclusion (excludes the source creature).
    characteristics = ["another"];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = new ObjectFilter
      {
        Supertypes = supertypes,
        Controller = ControllerFilter.You,
        Characteristics = characteristics,
      },
    };
  }
}
