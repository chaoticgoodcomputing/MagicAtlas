namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you sacrifice a [subtype]" — player-sacrifice trigger (Rule 701.21 Sacrifice; Rule 603).
///
/// Handles oracle text of the form "Whenever you sacrifice a [Subtype]", where [Subtype] is a
/// named permanent subtype such as Food, Treasure, Clue, or Blood. The filter carries the
/// sacrificed object's subtype and the controller (You), so "you sacrifice a Food" maps to
/// TriggerEvent.Sacrifices + Filter{ Subtypes:["Food"], Controller:You }.
///
/// CR 701.21a: "To sacrifice a permanent, its controller moves it from the battlefield directly
/// to its owner's graveyard. A player can't sacrifice something that isn't a permanent, or
/// something that's a permanent they don't control."
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger event,
/// that ability automatically triggers."
/// </summary>
[TriggerConditionRule(Priority = 979)]
public sealed class SacrificeConditionRule : ITriggerConditionRule
{
  // Matches "you sacrifice a [Subtype]" where Subtype is a single capitalized word.
  private static readonly Regex _youSacrificeSubtype = new(
    @"\byou\s+sacrifice\s+a\s+(?<subtype>[A-Za-z]+)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("sacrifice"))
    {
      return null;
    }

    if (!lower.Contains("you sacrifice"))
    {
      return null;
    }

    var m = _youSacrificeSubtype.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var subtype = m.Groups["subtype"].Value;
    // Title-case normalize
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Sacrifices,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
