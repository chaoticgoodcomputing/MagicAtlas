namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "As this [permanent] enters, choose [BasicLandType1] or [BasicLandType2]." — CR
/// 614.12's as-enters replacement binding a chosen BASIC LAND TYPE value (Roots of
/// Life: "As this enchantment enters, choose Island or Swamp."). Sibling of
/// <see cref="ChooseColorOnEntryRule"/> / <see cref="ChooseCreatureTypeOnEntryRule"/> /
/// <see cref="ChooseCardTypeOnEntryRule"/> but for a basic land type value (CR 305.6:
/// Plains, Island, Swamp, Mountain, Forest). Unlike the card-type sibling, the printed
/// disjunction here is frequently a real subset of the five basic land types (not the
/// full set), so this rule captures the literal options onto
/// <see cref="ChooseBasicLandTypeEffect.Options"/>.
///
/// <para>Anchored end-to-end and requires at least one recognized basic land type word
/// immediately after "choose", so this pattern is disjoint from "choose a color" /
/// "choose a creature type" / the card-type enumeration and does not shadow those
/// siblings.</para>
/// </summary>
[StaticRule(Priority = 942)]
public sealed class ChooseBasicLandTypeOnEntryRule : IStaticRule
{
  private const string BasicLandTypeWord = "Plains|Island|Swamp|Mountain|Forest";

  private static readonly Regex _chooseBasicLandTypeOnEntryPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+choose\s+"
      + @"(?<options>(?:"
      + BasicLandTypeWord
      + @")(?:,\s*(?:"
      + BasicLandTypeWord
      + @"))*(?:,?\s+or\s+(?:"
      + BasicLandTypeWord
      + @"))?)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _chooseBasicLandTypeOnEntryPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var options = Regex
      .Split(match.Groups["options"].Value, @",\s*|\s+or\s+")
      .Select(s => s.Trim())
      .Where(s => s.Length > 0)
      .ToList();

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new ChooseBasicLandTypeEffect { Options = options }],
      },
    ];
  }
}
