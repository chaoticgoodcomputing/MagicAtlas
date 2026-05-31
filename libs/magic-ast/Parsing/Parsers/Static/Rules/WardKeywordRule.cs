namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 989)]
public sealed class WardKeywordRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*Ward\s+(?<cost>(?:\{[^}]+\})+)\s*(?<rest>.*)$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var costStr = match.Groups["cost"].Value;
    MagicAST.AST.Costs.ManaCost? wardCost;
    try
    {
      var parsed = new MagicAST.Parsing.ManaCostParser().Parse(costStr);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      wardCost = new MagicAST.AST.Costs.ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    Parenthetical? reminder = null;
    var rest = match.Groups["rest"].Value.Trim();
    if (rest.StartsWith('(') && rest.EndsWith(')'))
    {
      reminder = new Parenthetical { Text = rest };
    }

    var trigger = new MagicAST.AST.Triggers.TriggerCondition
    {
      Timing = MagicAST.AST.Triggers.TriggerTiming.Whenever,
      Event = MagicAST.AST.Triggers.TriggerEvent.BecomesTarget,
      Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
    };

    var counterSpell = new MagicAST.AST.Effects.Core.PreventableEffect { Inner = new MagicAST.AST.Effects.Control.CounterSpellEffect {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It }}, Unless = new MagicAST.AST.Effects.UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        Cost = wardCost,
      } };

    return
    [
      new MagicAST.AST.Abilities.TriggeredAbility
      {
        KeywordSource = "Ward",
        Trigger = trigger,
        Effects = [counterSpell],
        Reminder = reminder,
      },
    ];
  }
}
