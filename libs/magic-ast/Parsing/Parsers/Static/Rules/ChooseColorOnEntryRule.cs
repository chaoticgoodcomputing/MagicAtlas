namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "As this [permanent] enters, choose a color." — CR 614.12's Voice of All
/// example ("As this creature enters, choose a color."). The affected-object
/// noun varies with the printed permanent type/subtype (e.g. "As this Aura
/// enters, choose a color." — Prismatic Ward, an Enchantment — Aura), so the
/// noun alternation lists every noun observed on this template rather than a
/// single fixed word.
/// </summary>
[StaticRule(Priority = 960)]
public sealed class ChooseColorOnEntryRule : IStaticRule
{
  private static readonly Regex _chooseColorOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment|Aura)\s+enters,\s+choose\s+a\s+color(?:\s+(?<restriction>other\s+than\s+[a-z]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseColorOnEntryPattern.Match(clause.RawText);
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
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Keyword.ChooseColorEffect
        {
          Restriction = restriction,
        }],
      },
    ];
  }
}
