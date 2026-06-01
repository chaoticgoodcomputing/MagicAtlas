namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever an opponent draws a card" — draw-card trigger scoped to an opponent
/// (CR 121.1: "A player draws a card by putting the top card of their library into
/// their hand"; CR 603.2: the event-match fires the ability whenever any opponent
/// performs that action).
/// The Filter Controller = Opponent narrows the trigger to draws performed by an
/// opponent rather than the controller.
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class OpponentDrawsCardConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("draw") || !lower.Contains("card"))
    {
      return null;
    }

    if (!Regex.IsMatch(lower, @"\ban\s+opponent\s+draws\s+a\s+card\b"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DrawsCard,
      Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
    };
  }
}
