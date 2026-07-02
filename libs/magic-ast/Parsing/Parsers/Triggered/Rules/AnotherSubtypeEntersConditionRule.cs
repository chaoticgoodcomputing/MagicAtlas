namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another [Subtype] you control enters" — subtype-filtered enters trigger.
/// Handles the Ally-rally pattern and analogous mechanics where the trigger
/// fires when another permanent of a specific creature subtype enters the
/// battlefield under your control. Rule 603.1: triggered abilities use "when,"
/// "whenever," or "at" to watch for zone changes; the subtype filter narrows
/// which entering permanents cause the ability to trigger.
///
/// Examples:
///   "Whenever another Ally you control enters, put a +1/+1 counter on this creature."
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class AnotherSubtypeEntersConditionRule : ITriggerConditionRule
{
  // Matches "another <Subtype> you control enters[[ the battlefield]]"
  // Subtype must be a proper-noun (capitalised first letter) to distinguish creature
  // subtypes ("Ally", "Vampire", "Spirit") from type words ("creature", "land").
  // Rule 205.3m: creature subtypes are capitalised in oracle text; card-type words
  // ("creature", "enchantment", "artifact") are lowercase. NOT using IgnoreCase so
  // "another creature you control enters" does NOT match here — that shape is
  // handled by the existing EntersConditionRule via ParseObjectFilter.
  private static readonly Regex _pattern = new(
    @"another\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+enters",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (oracle text capitalises creature subtypes — Rule 205.3m).
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
