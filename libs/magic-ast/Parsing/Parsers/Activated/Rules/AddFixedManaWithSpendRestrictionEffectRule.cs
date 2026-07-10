namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "Add [fixed mana symbols]. Spend this mana only to [restriction]." — Dalakos,
/// Crafter of Wonders: "{T}: Add {C}{C}. Spend this mana only to cast artifact
/// spells or activate abilities of artifacts."
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the
/// following criteria: it doesn't require a target (see rule 115.6), it could add
/// mana to a player's mana pool when it resolves, and it's not a loyalty ability."
/// The enclosing "{T}: Add …" ability is a mana ability. CR 106.4: "When an effect
/// instructs a player to add mana, that mana goes into a player's mana pool." — a
/// spend restriction on that mana (Rule 106.5 governs restricted mana) does not
/// change that.
/// </para>
///
/// <para>
/// Sibling to <see cref="AddManaEffectRule"/>'s "Add one mana of any color[.
/// Spend this mana only to &lt;X&gt;]" arm (Unclaimed Territory): that arm requires
/// the free-choice-of-color shape ("one mana of any color"), never fixed mana
/// symbols. <see cref="AddManaEffectRule"/> explicitly BAILS (returns null) on any
/// mana text containing "spend this mana only" that it does not itself recognise
/// (its <c>UnmodeledManaClause</c> guard), so this rule — matching the FIXED-
/// mana-symbols shape that guard declines — cannot shadow or be shadowed by it;
/// dispatch order between the two is immaterial (a new, disjoint file rather than
/// an edit to that shared rule body).
/// </para>
///
/// <para>
/// The restriction clause is captured verbatim into
/// <see cref="AddManaEffect.SpendRestriction"/> (its own doc: "MAST describes; it
/// does not execute — this holds the restriction text verbatim"), mirroring
/// Unclaimed Territory's shape exactly, just with a fixed <see cref="AddManaEffect.Mana"/>
/// symbol string instead of <see cref="AddManaEffect.AnyColor"/>.
/// </para>
///
/// <para>
/// Anchored (^…$) pattern requiring the literal "Add " lead-in, one-or-more curly-
/// brace mana symbols, then the literal "Spend this mana only to " tail — so it
/// cannot match any other effect shape as a substring.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1002)]
public sealed class AddFixedManaWithSpendRestrictionEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Add\s+(?<mana>(?:\{[^}]+\})+)\.\s+Spend\s+this\s+mana\s+only\s+to\s+(?<restriction>.+?)\.?\s*$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = _pattern.Match(effectText.Trim());
    if (!match.Success)
    {
      return null;
    }

    return new AddManaEffect
    {
      Mana = match.Groups["mana"].Value,
      AnyColor = false,
      SpendRestriction = match.Groups["restriction"].Value.Trim(),
    };
  }
}
