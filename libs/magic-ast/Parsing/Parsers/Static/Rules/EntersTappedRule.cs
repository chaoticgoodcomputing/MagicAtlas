namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 962)]
public sealed class EntersTappedRule : IStaticRule
{
  private static readonly Regex _entersTappedPattern = new(
    @"^\s*This\s+(?:permanent|land|creature|artifact|enchantment|spell)\s+enters\s+tapped"
    + @"(?:\s+unless\s+(?<condition>[^.]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _entersTappedIfConditionPattern = new(
    @"^\s*If\s+(?<condition>[^,]+),\s+this\s+(?:permanent|land|creature|artifact|enchantment|spell)\s+enters\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _entersTappedOpponentsCreaturesPattern = new(
    @"^\s*Creatures\s+your\s+opponents\s+control\s+enter\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // Arm 1: "This [permanent] enters tapped [unless condition]." (fastland/checkland)
    var match = _entersTappedPattern.Match(clause.RawText);
    if (match.Success)
    {
      var conditionGroup = match.Groups["condition"];
      Condition? entryCondition = conditionGroup.Success
        ? new Condition { Text = conditionGroup.Value.Trim() }
        : null;

      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Keyword.EntersTappedEffect { EntryCondition = entryCondition }],
        },
      ];
    }

    // Arm 2: "If [condition], this [permanent] enters tapped." (slow land)
    var ifMatch = _entersTappedIfConditionPattern.Match(clause.RawText);
    if (ifMatch.Success)
    {
      var conditionText = ifMatch.Groups["condition"].Value.Trim();
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Keyword.EntersTappedEffect
          {
            EntryCondition = new Condition { Text = conditionText },
            EntryConditionIsPositive = true,
          }],
        },
      ];
    }

    // Arm 3: "Creatures your opponents control enter tapped." — Blind Obedience
    // creature variant (Rule 614). The ability is scoped to creatures controlled
    // by opponents rather than the source permanent itself. The scope is encoded
    // as an ObjectFilter on EntersTappedEffect.Scope so downstream consumers can
    // distinguish "this card enters tapped" (Scope=null) from "opponent creatures
    // enter tapped" (Scope.Controller=Opponent).
    if (_entersTappedOpponentsCreaturesPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Keyword.EntersTappedEffect
          {
            Scope = new ObjectFilter
            {
              CardTypes = ["creature"],
              Controller = ControllerFilter.Opponent,
            },
          }],
        },
      ];
    }

    return null;
  }
}
