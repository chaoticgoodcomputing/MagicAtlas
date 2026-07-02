namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you gain life" — life-gain trigger. Controller defaults to You,
/// flips to Opponent when the oracle text names an opponent.
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class GainsLifeConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("gain") || !lower.Contains("life"))
    {
      return null;
    }

    if (!Regex.IsMatch(lower, @"\b(you|opponent|a player)\s+gain(s)?\s+life\b"))
    {
      return null;
    }

    ControllerFilter controller = lower.Contains("opponent")
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.GainsLife,
      Filter = new ObjectFilter { Controller = controller },
    };
  }
}
