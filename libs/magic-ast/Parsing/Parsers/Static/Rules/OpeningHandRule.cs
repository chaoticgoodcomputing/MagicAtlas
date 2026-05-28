namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 943)]
public sealed class OpeningHandRule : IStaticRule
{
  // Matches the exact Leyline oracle line: "If this card is in your opening
  // hand, you may begin the game with it on the battlefield."
  // The period is optional to tolerate minor formatting variants.
  private static readonly Regex _openingHandPattern = new(
    @"^\s*If\s+this\s+card\s+is\s+in\s+your\s+opening\s+hand,\s+you\s+may\s+begin\s+the\s+game\s+with\s+it\s+on\s+the\s+battlefield\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_openingHandPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Timing.OpeningHandEffect
        {
          IsOptional = true,
        }],
      },
    ];
  }
}
