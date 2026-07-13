namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;

/// <summary>
/// "Add [N] mana in any combination of colors." — a fixed-count "any combination"
/// mana production where N is a literal word-number (Chandra, Hope's Beacon's
/// "+2: Add two mana in any combination of colors.").
///
/// <para>
/// The general <see cref="AddManaEffectRule"/> already models the <em>variable</em>
/// form "X mana in any combination of {…}" (ADR 0009 S3), but its regex anchors on a
/// literal "X" count and its flat-mana path bails on text without a '{' symbol, so it
/// does not claim this word-count "colors" phrasing. This rule fills that gap: the
/// produced mana is <see cref="Amount"/> units (a <see cref="LiteralQuantity"/>), each
/// freely chosen from the five colors, so <see cref="AddManaEffect.Mana"/> is <c>""</c>
/// and the color set rides <see cref="AddManaEffect.AnyCombinationOf"/> — never
/// free-texted into the Mana scalar.
/// </para>
///
/// <para>
/// CR 106.4: "When an effect instructs a player to add mana, that mana goes into a
/// player's mana pool." The enclosing ability is a loyalty ability, so per CR 605.1a
/// it is NOT a mana ability — <see cref="ActivatedAbilityParser"/> forces
/// <c>IsManaAbility = false</c> for loyalty abilities regardless of this effect.
/// </para>
///
/// <para>ANCHORED (<c>^…$</c>): the whole post-colon sentence must be this shape, so
/// the rule cannot match a substring of a larger effect.</para>
/// </summary>
[ActivatedEffectRule(Priority = 960)]
public sealed class AddNManaAnyCombinationOfColorsEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Add\s+(?<word>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+mana\s+in\s+any\s+combination\s+of\s+colors$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var m = Pattern.Match(effectText.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return null;
    }

    var count = ActivatedRuleHelpers.ParseNumberWord(m.Groups["word"].Value) ?? 0;
    if (count <= 0)
    {
      return null;
    }

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      Amount = LiteralQuantity.Of(count),
      AnyCombinationOf = ["W", "U", "B", "R", "G"],
    };
  }
}
