namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 998)]
public sealed class MustBeBlockedRule : IStaticRule
{
  private static readonly Regex _mustBeBlockedPattern = new(
    @"^\s*\S.*?\s+must\s+be\s+blocked\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_mustBeBlockedPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MustBeBlockedEffect { Target = ObjectReference.Self() }],
      },
    ];
  }
}
