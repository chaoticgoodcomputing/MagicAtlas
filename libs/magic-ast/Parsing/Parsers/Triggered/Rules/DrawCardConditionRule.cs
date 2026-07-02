namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you draw a card" — draw-card trigger (Rule 121: Drawing a Card).
/// Fires whenever the controller draws a card by any means. Controller defaults to You.
/// </summary>
[TriggerConditionRule(Priority = 996)]
public sealed class DrawCardConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("draw") || !lower.Contains("card"))
    {
      return null;
    }

    if (!Regex.IsMatch(lower, @"\byou\s+draw\s+a\s+card\b"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DrawsCard,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }
}
