namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;

/// <summary>
/// "[Name] enters prepared." — the bare self-by-name prepared-designation static
/// ability printed on the front face of prepare-layout double-faced cards that
/// name themselves rather than using "This creature" (e.g. "Sanar enters
/// prepared."). Sibling of <see cref="SelfNameEntersTappedRule"/> for the
/// self-by-name convention; the "This creature enters prepared." form is already
/// recognised by <see cref="MagicAST.Keywords.Definitions.PreparedKeyword"/>'s
/// token-level combinator (dispatched via <see cref="KeywordListRule"/>), which
/// cannot match a literal card name since the tokenizer has no access to it. This
/// rule closes that gap with the same regex-on-raw-text technique used for the
/// enters-tapped self-by-name sibling.
///
/// <para>
/// CR 722.3a: "Some spells and abilities cause a permanent with a prepare spell to
/// become prepared or state that a permanent enters prepared. If that permanent
/// has the alternative characteristics of a prepare spell, this gives the
/// permanent the 'prepared' designation. Prepared is a designation that acts as a
/// marker which rules and effects can identify."
/// </para>
///
/// <para>
/// Produces the identical <see cref="StaticAbility"/> shape as
/// <see cref="MagicAST.Keywords.Definitions.PreparedKeyword"/>'s expansion
/// (<c>KeywordSource = Prepared</c>, a single <see cref="KeywordAbilityEffect"/>)
/// so the two recognition paths ("This creature enters prepared." vs.
/// "[Name] enters prepared.") stay indistinguishable downstream.
/// </para>
///
/// <para>
/// Anchored (^…$) immediately after "enters prepared." (with an optional trailing
/// reminder parenthetical) so this cannot steal a substring of a longer clause.
/// A negative lookahead excludes a leading "This" so this rule stays disjoint from
/// <see cref="KeywordListRule"/>'s "This creature enters prepared." shape even if
/// dispatch order ever changes; the required "enters" (with the s) restricts to
/// the singular self form.
/// </para>
/// </summary>
[StaticRule(Priority = 960)]
public sealed class SelfNameEntersPreparedRule : IStaticRule
{
  // Self-by-name bare form: "[CardName] enters prepared." — optionally followed
  // by a reminder-text parenthetical, e.g. "Sanar enters prepared. (While it's
  // prepared, you may cast a copy of its spell. Doing so unprepares it.)"
  private static readonly Regex _pattern = new(
    @"^\s*(?!This\b)[A-Z][A-Za-z'\-]+(?:,\s+[A-Z][A-Za-z'\-]+)*(?:\s+[A-Za-z'\-]+)*\s+enters\s+prepared\.?(?:\s*\(.*\))?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    var entersPrepared = new StaticAbility
    {
      KeywordSource = KeywordAbility.Prepared,
      Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Prepared }],
    };

    return [entersPrepared];
  }
}
