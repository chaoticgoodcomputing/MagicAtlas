namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 954)]
public sealed class BlockAdditionalRule : IStaticRule
{
  private static readonly Regex _blockAnyNumberPattern = new(
    @"^\s*This\s+creature\s+can\s+block\s+any\s+number\s+of\s+creatures\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _blockAdditionalPattern = new(
    @"^\s*This\s+creature\s+can\s+block\s+an\s+additional\s+creature\s+each\s+combat\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (_blockAnyNumberPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Combat.BlockAdditionalEffect { IsUnlimited = true }],
        },
      ];
    }
    if (!_blockAdditionalPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Combat.BlockAdditionalEffect()],
      },
    ];
  }
}
