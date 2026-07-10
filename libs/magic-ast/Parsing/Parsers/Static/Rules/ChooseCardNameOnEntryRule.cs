namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "As this [permanent] enters, choose a card name." (Declaration of Naught) —
/// recognizes the as-enters card-name-choice declaration and emits a composite
/// <see cref="StaticAbility"/> carrying <see cref="StaticTimingKind.AsThisEnters"/>
/// (CR 614.12) plus a plain <see cref="MagicAST.AST.Effects.Keyword.ChooseCardNameEffect"/>.
/// Sibling of <see cref="ChoosePlayerOnEntryRule"/> / <see cref="ChooseColorOnEntryRule"/> /
/// <see cref="ChooseCreatureTypeOnEntryRule"/>: the regexes for "choose a card
/// name"/"choose a player"/"choose a color"/"choose a creature type" are disjoint,
/// so dispatch priority relative to those rules is immaterial.
/// </summary>
[StaticRule(Priority = 942)]
public sealed class ChooseCardNameOnEntryRule : IStaticRule
{
  private static readonly Regex _chooseCardNameOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+a\s+card\s+name\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseCardNameOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Keyword.ChooseCardNameEffect()],
      },
    ];
  }
}
