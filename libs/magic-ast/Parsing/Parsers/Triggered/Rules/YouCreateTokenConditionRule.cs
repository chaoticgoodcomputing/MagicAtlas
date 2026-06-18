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
  // Non-anchored: the trigger text passed in includes the timing word ("Whenever"),
  // so an anchored pattern would never match. Instead, match the body phrase with
  // word boundaries so we do not fire on a compound like
  // "you create a token for each opponent dealt damage" (a different shape).
  // The terminal \b ensures "token" is not a prefix of a longer word, and the
  // negative lookahead (?!\s+for\s) guards the "for each" compound (which produces
  // a different CreateThatManyTreasureTokensRule shape). "tokens" (plural) is allowed
  // for future pluralised oracle forms even though the Rosie Cotton text is singular.
  private static readonly Regex _pattern = new(
    @"\byou\s+create\s+a\s+tokens?\b(?!\s+for\b)",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("create"))
    {
      return null;
    }

    if (!lower.Contains("token"))
    {
      return null;
    }

    if (!_pattern.IsMatch(lower))
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
