namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 988)]
public sealed class CostReductionWhereXRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+spell\s+costs\s+\{X\}\s+less\s+to\s+cast,\s+where\s+X\s+is\s+(?:the\s+total\s+amount\s+of\s+)?(?<source>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var source = match.Groups["source"].Value.Trim();
    var derivedKind = source.Contains("damage", StringComparison.OrdinalIgnoreCase)
      ? MagicAST.AST.Quantities.DerivedKind.DamageDealt
      : MagicAST.AST.Quantities.DerivedKind.Other;
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = new MagicAST.AST.Quantities.DerivedQuantity
          {
            DerivedFrom = derivedKind,
            Source = source,
          },
          BasedOn = source,
        }],
      },
    ];
  }
}
