namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 996)]
public sealed class MustBlockRule : IStaticRule
{
  private static readonly Regex _mustBlockPattern = new(
    @"^\s*(?<subject>\S.*?)\s+blocks?\s+each\s+combat\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _mustBlockPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var target = StaticRuleHelpers.ClassifyCombatRequirementSubject(match.Groups["subject"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new MustBlockEffect { Target = target }],
      },
    ];
  }
}
