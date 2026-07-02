namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you scry or surveil" — fires on either keyword action (Rule 701.18 / 701.43).
/// </summary>
[TriggerConditionRule(Priority = 995)]
public sealed class ScryOrSurveilConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("scry") && !lower.Contains("surveil"))
    {
      return null;
    }

    if (!Regex.IsMatch(lower, @"\byou\s+scry\s+or\s+surveil\b"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ScryOrSurveil,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }
}
