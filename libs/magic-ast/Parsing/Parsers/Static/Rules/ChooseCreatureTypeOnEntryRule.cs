namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "As this [permanent] enters, choose a creature type." — recognizes the
/// as-enters creature-type-choice declaration (Unclaimed Territory shape) and
/// emits a <see cref="MagicAST.AST.Effects.Keyword.ChooseCreatureTypeOnEntryEffect"/>.
/// Sibling of <see cref="ChooseColorOnEntryRule"/>; the two regexes are disjoint
/// ("choose a color" vs "choose a creature type"), so dispatch priority relative
/// to the color rule is immaterial.
/// </summary>
[StaticRule(Priority = 941)]
public sealed class ChooseCreatureTypeOnEntryRule : IStaticRule
{
  private static readonly Regex _chooseCreatureTypeOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+a\s+creature\s+type(?:\s+(?<restriction>other\s+than\s+[A-Za-z]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseCreatureTypeOnEntryPattern.Match(clause.RawText);
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
        Effects = [new MagicAST.AST.Effects.Keyword.ChooseCreatureTypeOnEntryEffect
        {
          Restriction = restriction,
        }],
      },
    ];
  }
}
