namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

/// <summary>
/// "As this [permanent] enters, choose artifact, creature, enchantment, instant, or sorcery."
/// — recognizes the as-enters card-type-choice declaration (Cloud Key shape) and emits a
/// composite <see cref="StaticAbility"/> carrying
/// <see cref="StaticTimingKind.AsThisEnters"/> plus a plain
/// <see cref="MagicAST.AST.Effects.Keyword.ChooseCardTypeEffect"/>.
///
/// <para>The oracle text lists the options as a disjunction ("artifact, creature, enchantment,
/// instant, or sorcery") — the specific enumeration is reminder context, not structured data, so
/// no <c>Options</c> field is needed: the rule captures any mention of those permanent types in
/// a disjunctive list. A loose anchored pattern is used (does not require the exact five-type
/// enumeration) so that future printings with different subsets still parse correctly.</para>
///
/// <para>Sibling of <see cref="ChooseCreatureTypeOnEntryRule"/> and
/// <see cref="ChooseColorOnEntryRule"/>; priority 940 is below both siblings so they claim
/// their distinct noun phrases first and this rule does not shadow them.</para>
/// </summary>
[StaticRule(Priority = 940)]
public sealed class ChooseCardTypeOnEntryRule : IStaticRule
{
  // Matches "As this [permanent type] enters, choose [list of card types]."
  // The list must contain at least one of the five spell/permanent card types —
  // artifact, creature, enchantment, instant, or sorcery — as the leading word,
  // so this pattern does NOT fire for "choose a color" or "choose a creature type".
  private static readonly Regex _chooseCardTypeOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+"
      + @"(?:artifact|creature|enchantment|instant|sorcery)"
      + @"(?:[^.]+)?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseCardTypeOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Keyword.ChooseCardTypeEffect()],
      },
    ];
  }
}
