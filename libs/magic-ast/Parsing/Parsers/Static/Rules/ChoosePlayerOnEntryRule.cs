namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "As this [permanent] enters, choose a player." (Sawhorn Nemesis) — recognizes
/// the as-enters player-choice declaration and emits a composite
/// <see cref="StaticAbility"/> carrying <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 614.1c) plus a plain <see cref="MagicAST.AST.Effects.Keyword.ChoosePlayerEffect"/>.
/// Sibling of <see cref="ChooseColorOnEntryRule"/> / <see cref="ChooseCreatureTypeOnEntryRule"/>:
/// the regexes for "choose a color"/"choose a creature type"/"choose a player" are
/// disjoint, so dispatch priority relative to those rules is immaterial.
/// </summary>
[StaticRule(Priority = 942)]
public sealed class ChoosePlayerOnEntryRule : IStaticRule
{
  private static readonly Regex _choosePlayerOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+a\s+player\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _choosePlayerOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Keyword.ChoosePlayerEffect()],
      },
    ];
  }
}
