namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 944)]
public sealed class PlayAdditionalLandRule : IStaticRule
{
  // Matches "You may play an additional land on each of your turns."
  // The trailing period is optional for minor formatting variants.
  private static readonly Regex _playAdditionalLandPattern = new(
    @"^\s*You\s+may\s+play\s+an\s+additional\s+land\s+on\s+each\s+of\s+your\s+turns\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_playAdditionalLandPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Keyword.PlayAdditionalLandEffect
        {
          IsOptional = true,
        }],
      },
    ];
  }
}
