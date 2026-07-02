namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you commit a crime" — the crime-mechanic trigger (CR 700.13).
///
/// <para>
/// CR 700.13: "A player commits a crime as that player casts a spell, activates
/// an ability, or puts a triggered ability on the stack and that spell or ability
/// targets at least one opponent; at least one permanent, spell, or ability an
/// opponent controls; and/or at least one card in an opponent's graveyard."
/// </para>
///
/// <para>
/// The trigger fires on the controller ("you"), encoded as
/// <see cref="ControllerFilter.You"/> on the filter. The crime definition is
/// purely a rules term (CR 700.13); its expansion is engine territory. MAST
/// records only the canonical event label <see cref="TriggerEvent.CommitsACrime"/>
/// and the controller constraint.
/// </para>
///
/// <para>
/// The phrase "Whenever you commit a crime" is unique and cannot appear as a
/// substring of a more-specific trigger phrase, so no priority elevation is
/// needed. The anchor on <see cref="_pattern"/> is full-string (^…$) to guard
/// against future sibling triggers that might extend the phrase.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class CommitsCrimeConditionRule : ITriggerConditionRule
{
  /// <summary>
  /// Matches: "you commit a crime" anywhere in the trigger text.
  /// The dispatcher passes the full trigger phrase (e.g. "Whenever you commit a crime"),
  /// so we use a word-boundary match rather than anchoring with ^. The "crime" guard
  /// ensures early exit for unrelated triggers before the regex is evaluated.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"\byou\s+commit\s+a\s+crime\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("crime"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CommitsACrime,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }
}
