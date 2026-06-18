namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you discard a card" — discard-card trigger (CR 701.9a: "To discard a
/// card, move it from its owner's hand to that player's graveyard.").
///
/// <para>
/// Fires whenever the controller discards a card by any means — from cost payment,
/// from a discard effect, from a forced discard, etc. (CR 603.2: the trigger fires
/// on the event, regardless of cause). The controller defaults to You.
/// </para>
///
/// <para>
/// CR 603: "Triggered abilities have a trigger condition and an effect. They are
/// written as '[When/Whenever/At] [trigger condition or event], [effect].'"
/// CR 701.9a defines the discard action. Event = DiscardsCard, Filter.Controller = You.
/// </para>
///
/// <para>
/// Anchored check prevents matching "an opponent discards a card" or other
/// subject-prefixed discard forms — those route to <c>EachOpponentDiscardsRule</c>
/// or <c>TargetOpponentDiscardsTriggeredRule</c>.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 980)]
public sealed class YouDiscardCardConditionRule : ITriggerConditionRule
{
  // Word-boundary match: subject is "you discard a card".
  // Not front-anchored because the trigger text includes the timing word (e.g. "Whenever you discard a card").
  // Tail-anchored ($) to prevent matching "you discard a card and draw a card" as this condition.
  private static readonly Regex Pattern = new(
    @"\byou\s+discard\s+a\s+card\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("discard") || !lower.Contains("card"))
    {
      return null;
    }

    if (!Pattern.IsMatch(lower))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DiscardsCard,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }
}
