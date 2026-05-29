namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 971)]
public sealed class CantBeCastRestrictionRule : IStaticRule
{
  private static readonly Regex _cantBeCastManaValuePattern = new(
    @"^\s*Noncreature\s+spells\s+with\s+mana\s+value\s+(?<value>\d+)\s+or\s+greater\s+can'?t\s+be\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeCastXInCostPattern = new(
    @"^\s*Noncreature\s+spells\s+with\s+\{X\}\s+in\s+their\s+mana\s+costs\s+can'?t\s+be\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var mvMatch = _cantBeCastManaValuePattern.Match(clause.RawText);
    if (mvMatch.Success)
    {
      var value = int.Parse(mvMatch.Groups["value"].Value);
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Timing.CantBeCastEffect()],
          AffectedObjects = new ObjectFilter
          {
            CardTypes = ["spell"],
            Characteristics = [Characteristic.Other("noncreature")],
            ManaValueComparison = new Comparison
            {
              Operator = ComparisonOperator.GreaterThanOrEqual,
              Value = value,
            },
          },
        },
      ];
    }

    if (_cantBeCastXInCostPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Timing.CantBeCastEffect()],
          AffectedObjects = new ObjectFilter
          {
            CardTypes = ["spell"],
            Characteristics = [Characteristic.Other("noncreature"), Characteristic.Other("with {X} in their mana costs")],
          },
        },
      ];
    }

    return null;
  }
}
