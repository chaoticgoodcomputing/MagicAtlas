namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 963)]
public sealed class EntersTappedAndChooseColorRule : IStaticRule
{
  private static readonly Regex _entersTappedAndChooseColorPattern = new(
    @"^\s*This\s+(?:permanent|land|creature|artifact|enchantment)\s+enters\s+tapped\."
    + @"\s+As\s+it\s+enters,\s+choose\s+a\s+color"
    + @"(?:\s+(?<restriction>other\s+than\s+[a-z]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _entersTappedAndChooseColorPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var restrictionGroup = match.Groups["restriction"];
    string? restriction = restrictionGroup.Success
      ? restrictionGroup.Value.Trim()
      : null;

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MagicAST.AST.Effects.Keyword.EntersTappedEffect(),
          new MagicAST.AST.Effects.Keyword.ChooseColorOnEntryEffect
          {
            Restriction = restriction,
          },
        ],
      },
    ];
  }
}
