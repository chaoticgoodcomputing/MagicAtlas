namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 959)]
public sealed class CantBlockRule : IStaticRule
{
  private static readonly Regex _cantBlockPattern = new(
    @"^\s*This\s+(?:creature|land|permanent)\s+can'?t\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_cantBlockPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Combat.CantBlockEffect()],
      },
    ];
  }
}
