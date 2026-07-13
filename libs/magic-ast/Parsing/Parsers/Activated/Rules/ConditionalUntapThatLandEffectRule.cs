namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// "[Then ]if [condition], untap that land." — the conditional reflexive untap that
/// trails the search-to-battlefield fetch on Fabled Passage:
/// "Search your library for a basic land card, put it onto the battlefield tapped,
/// then shuffle. Then if you control four or more lands, untap that land."
///
/// <para>
/// The search-and-shuffle sentence is handled by
/// <see cref="SearchLibraryToBattlefieldEffectRule"/>; the activated-ability parser
/// splits the effect half on sentence boundaries, so this rule structures the second
/// sentence into a <see cref="ConditionalEffect"/> gating an <see cref="UntapEffect"/>.
/// </para>
///
/// <para>
/// "that land" is the land the preceding search put onto the battlefield tapped — a
/// back-reference to the object mentioned earlier in the same ability, modelled as
/// <see cref="ObjectReferenceKind.It"/> (reference-not-resolution, ADR 0004). The
/// condition ("you control four or more lands") is delegated verbatim to
/// <see cref="ConditionParser"/>, which recognises the dominant count shape as a
/// <see cref="MagicAST.AST.Abilities.CountCondition"/>.
/// </para>
///
/// CR 701.26b: "To untap a permanent, rotate it back to the upright position from a
/// sideways position. Only tapped permanents can be untapped."
/// CR 305.6 (basic land types) and CR 701.23a (search) govern the paired search sentence.
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class ConditionalUntapThatLandEffectRule : IActivatedEffectRule
{
  // Anchored (^…$) so it only matches the whole "[Then ]if <cond>, untap that land"
  // sentence and can never claim a substring of a longer clause. The optional leading
  // "then" is the sequencing connector between the fetch sentence and this one.
  private static readonly Regex Pattern = new(
    @"^(?:then\s+)?if\s+(?<cond>.+?),\s+untap\s+that\s+land$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var condition = ConditionParser.Parse(m.Groups["cond"].Value.Trim());

    return new ConditionalEffect
    {
      Condition = condition,
      Then = new UntapEffect { Target = ObjectReference.It() },
    };
  }
}
