namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing;

[StaticRule(Priority = 949)]
public sealed class SkipUntapRule : IStaticRule
{
  // Matches "You may choose not to untap this [permanent-type] during your untap step."
  // The permanent-type noun is flexible — oracle uses the card's own type ("this
  // artifact", "this creature", "this permanent", etc.). The trailing period is
  // optional for minor formatting variants.
  private static readonly Regex _skipUntapPattern = new(
    @"^\s*You\s+may\s+choose\s+not\s+to\s+untap\s+this\s+(?:creature|permanent|artifact|enchantment|land)\s+during\s+your\s+untap\s+step\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_skipUntapPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Timing.SkipUntapEffect {}],
      },
    ];
  }
}
