namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// S10 — "For each color among permanents you control, add one mana of that color."
/// (Bloom Tender / Vivid ability-word shape, Eventide).
///
/// <para>
/// The effect produces one mana of each distinct color present among the controller's
/// permanents on the battlefield — a per-color loop rather than a fixed symbol or a
/// "choose one" free pick. This is modelled with
/// <see cref="AddManaEffect.ForEachColorAmong"/> carrying an <see cref="ObjectFilter"/>
/// for controller-scoped permanents, and <see cref="AddManaEffect.Mana"/> left as
/// <c>""</c> because the colors are determined at resolution. The outer ability carries
/// <c>IsManaAbility = true</c> (CR 605.1a: no target, could add mana, not loyalty).
/// </para>
///
/// <para>
/// CR 207.2c: "Vivid" is an ability word — it appears in italics before the em-dash
/// and has no special rules meaning. The classifier strips the "Vivid — " prefix so
/// this rule receives only the post-colon effect text.
/// </para>
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the following
/// criteria: it doesn't require a target (see rule 115.6), it could add mana to a
/// player's mana pool when it resolves, and it's not a loyalty ability." The
/// "permanents you control" filter is not a target (CR 115.6); the ability satisfies
/// all three criteria.
/// </para>
///
/// <para>
/// Runs at Priority = 1030 — above <see cref="AddManaForEachControlledCreatureEffectRule"/>
/// (1020) and <see cref="AddManaForEachControlledSubtypeEffectRule"/> (1010) so the
/// color-loop shape wins before any "Add {mana} for each …" shape in
/// <see cref="AddManaEffectRule"/>. The regex is anchored at both ends
/// (^…$) so it cannot match as a substring of a more-specific sibling.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1030)]
public sealed class AddManaForEachColorAmongControlledPermanentsEffectRule : IActivatedEffectRule
{
  // Anchored at both ends. Matches only the exact Bloom Tender-family surface phrase.
  // "permanents you control" is the only textual variant printed by WotC for this mechanic.
  // The leading "For each" is the grammatical subject; "add one mana of that color" is the
  // instruction — structurally a loop, not a simple "Add {symbol}".
  private static readonly Regex ForEachColorAmongControlled = new(
    @"^For\s+each\s+color\s+among\s+permanents\s+you\s+control,\s+add\s+one\s+mana\s+of\s+that\s+color\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = ForEachColorAmongControlled.Match(effectText.Trim());
    if (!match.Success)
      return null;

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      ForEachColorAmong = new ObjectFilter
      {
        CardTypes = ["permanent"],
        Controller = ControllerFilter.You,
        Zone = Zone.Battlefield,
      },
    };
  }
}
