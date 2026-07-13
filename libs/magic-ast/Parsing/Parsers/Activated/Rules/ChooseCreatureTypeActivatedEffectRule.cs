namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "Choose a creature type[ other than X]." — the creature-type-choice declaration as
/// a stand-alone sentence inside an activated ability's effect half (Imagecrafter:
/// "{T}: Choose a creature type other than Wall. Target creature becomes that type
/// until end of turn."). Emits a plain
/// <see cref="ChooseCreatureTypeEffect"/> carrying the optional exclusion in
/// <see cref="ChooseCreatureTypeEffect.Restriction"/>.
///
/// <para>
/// This is the activated-ability sibling of the static
/// <c>ChooseCreatureTypeOnEntryRule</c> ("As this land enters, choose a creature
/// type …", CR 614.1c). The two live in different parsers (static vs activated) and
/// their anchored regexes are disjoint, so they never compete. When this sentence is
/// followed by a "becomes that type" sentence, the activated parser's multi-sentence
/// splitter parses each independently and concatenates them; this effect is the
/// producer of the choice, and the later <c>ChangeSubtypeEffect</c> back-references it
/// (CR 607.1 linked abilities). MAST models only the choice declaration itself, not
/// the producer/consumer link.
/// </para>
///
/// <para>
/// The restriction is captured verbatim (e.g. "other than Wall"), matching the format
/// stored by <c>ChooseCreatureTypeOnEntryRule</c>. CR 205.3 constrains a creature-type
/// choice to a single creature type ("'Goblin' … is a creature type"); the exclusion
/// merely removes one candidate.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class ChooseCreatureTypeActivatedEffectRule : IActivatedEffectRule
{
  // Anchored: the whole sentence must be "Choose a creature type" with an optional
  // "other than <Type>" exclusion. Trailing period is stripped before matching.
  private static readonly Regex _pattern = new(
    @"^Choose\s+a\s+creature\s+type(?:\s+(?<restriction>other\s+than\s+[A-Za-z]+))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var restrictionGroup = match.Groups["restriction"];
    return new ChooseCreatureTypeEffect
    {
      Restriction = restrictionGroup.Success ? restrictionGroup.Value.Trim() : null,
    };
  }
}
