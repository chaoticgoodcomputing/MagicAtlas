namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When you control no [LandSubtype]" — state-triggered ability (Rule 603). Fires
/// when the controlling player transitions to controlling no lands of the named
/// basic-land subtype (e.g. "When you control no Islands, sacrifice this creature.").
/// Expanded oracle for the obsolete Islandhome family. The land subtype is stored in
/// Filter.Subtypes; CardTypes=["land"], Controller=You.
/// <para>Supported subtypes: Island, Forest, Swamp, Mountain, Plains.</para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class ControlNoLandTypeConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("you control no"))
    {
      return null;
    }

    // Pattern: "When you control no [LandSubtype]". The leading trigger timing word
    // (When/Whenever) is still present on triggerText.
    var match = Regex.Match(
      triggerText,
      @"^(?:When|Whenever)\s+you\s+control\s+no\s+(?<subtype>Islands?|Forests?|Swamps?|Mountains?|Plains)\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    // Normalise to the canonical singular form ("Islands" → "Island", etc.).
    var raw = match.Groups["subtype"].Value.Trim();
    var subtype = raw.TrimEnd('s');
    // "Plains" ends in 's' but is already singular — restore it.
    if (raw.Equals("Plains", StringComparison.OrdinalIgnoreCase))
    {
      subtype = "Plains";
    }
    // Capitalise first letter to match the land-subtype proper-noun convention.
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ControlNoLandType,
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
