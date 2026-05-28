namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 947)]
public sealed class NoMaxHandSizeRule : IStaticRule
{
  private static readonly Regex _noMaxHandSizePattern = new(
    @"^\s*You\s+have\s+no\s+maximum\s+hand\s+size\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_noMaxHandSizePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Keyword.NoMaxHandSizeEffect()],
      },
    ];
  }
}
