namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you create a token" — token-creation trigger (CR 111.1; CR 603.2).
///
/// Fires whenever the ability's controller creates one or more tokens. The filter
/// carries Controller=You so that a "whenever any player creates a token" variant
/// (if ever added) is distinguishable. The anchor is tight:
///   ^you\s+create\s+a\s+token$
/// to avoid matching "you create a token for each…" compound forms (different shape)
/// or occurrences embedded in a broader trigger body.
///
/// CR 111.1: "Some effects put tokens onto the battlefield. A token is a marker used
/// to represent any permanent that isn't represented by a card."
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers."
/// </summary>
[TriggerConditionRule(Priority = 975)]
public sealed class YouCreateTokenConditionRule : ITriggerConditionRule
{
  // Strip the leading timing word, then require the body to be EXACTLY "you create a token".
  // ANCHORED (^…$) on the stripped body so a disjunctive/compound trigger is NOT matched as a
  // substring: Tchotchke Elemental's "you create a token OR put a counter OR a sticker on another
  // permanent" is a three-way trigger disjunction — firing on the bare "you create a token" piece
  // would silently DROP the counter/sticker disjuncts (CR 603.2 trigger-event accuracy). Such a
  // compound has no faithful single-event shape here, so it correctly falls through to unparsed.
  private static readonly Regex _timingPrefix = new(
    @"^(?:when(?:ever)?|at)\s+",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _pattern = new(
    @"^you\s+create\s+an?\s+tokens?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("create") || !lower.Contains("token"))
    {
      return null;
    }

    var body = _timingPrefix.Replace(triggerText.Trim(), string.Empty).Trim();
    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.TokenCreated,
      Filter = new ObjectFilter
      {
        Controller = ControllerFilter.You,
      },
    };
  }
}
