namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you sacrifice one or more [Subtype]s" — player-sacrifice trigger for a
/// batch of named-subtype permanents (Rule 701.21; Rule 603).
///
/// <para>
/// Handles oracle text of the form "Whenever you sacrifice one or more [Subtype]s,",
/// where [Subtype] is a named permanent subtype (e.g. Food, Treasure, Clue). The
/// "one or more" qualifier sets <see cref="TriggerCondition.MinimumCount"/> = 1,
/// matching the CR convention that the ability fires on any positive-count sacrifice
/// event regardless of exact quantity. The filter carries the sacrificed object's
/// subtype and the controller (You), so "you sacrifice one or more Foods" maps to
/// <see cref="TriggerEvent.Sacrifices"/> + Filter{ Subtypes:["Food"], Controller:You }
/// with MinimumCount = 1.
/// </para>
///
/// <para>
/// Distinct from <see cref="SacrificeConditionRule"/> which handles the singular
/// "you sacrifice a [Subtype]" form (no MinimumCount qualifier). Both rules share
/// the same TriggerEvent; the MinimumCount distinguishes "one or more" (batch) from
/// the singular form (any single instance).
/// </para>
///
/// <para>
/// CR 701.21a: "To sacrifice a permanent, its controller moves it from the battlefield
/// directly to its owner's graveyard."
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 980)]
public sealed class SacrificeOneOrMoreSubtypeConditionRule : ITriggerConditionRule
{
  // "you sacrifice one or more [SubtypeP]" — matches as a substring of the
  // full trigger text (e.g. "Whenever you sacrifice one or more Foods").
  // The plural subtype noun uses [A-Za-z]+ because "Foods" has a simple -s plural.
  // Word-boundary anchors (\b) prevent the phrase from matching inside a longer
  // trigger clause that would be handled by a more-specific sibling rule.
  private static readonly Regex _pattern = new(
    @"\byou\s+sacrifice\s+one\s+or\s+more\s+(?<subtypes>[A-Za-z]+)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("sacrifice") || !lower.Contains("one or more"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    // Depluralise the subtype (e.g. "Foods" → "Food", "Treasures" → "Treasure").
    // Oracle text uses the plural form in "one or more [Subtype]s"; the filter's
    // Subtypes array holds the singular canonical name.
    var plural = m.Groups["subtypes"].Value;
    var singular = plural.EndsWith('s') || plural.EndsWith('S')
      ? plural[..^1]
      : plural;
    // Normalise capitalisation: first letter upper, rest lower.
    singular = char.ToUpperInvariant(singular[0]) + singular[1..].ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Sacrifices,
      Filter = new ObjectFilter
      {
        Subtypes = [singular],
        Controller = ControllerFilter.You,
      },
      MinimumCount = 1,
    };
  }
}
