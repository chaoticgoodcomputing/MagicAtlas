namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "Add one mana of any color among [filter] you control." — the Mox Amber shape.
///
/// <para>
/// This rule handles the "one mana of any color among [filter]" family where the
/// controller may add one mana of freely-chosen color, but that color must be present
/// among a specific set of permanents they control. The canonical example is Mox Amber:
/// "Add one mana of any color among legendary creatures and planeswalkers you control."
/// </para>
///
/// <para>
/// Distinct from <see cref="AddManaForEachColorAmongControlledPermanentsEffectRule"/>
/// (Bloom Tender — adds one mana per EACH distinct color present) and from
/// <see cref="AddManaEffectRule"/>'s "one mana of any color" branch (unconstrained —
/// any of the five colors). Here the color is freely chosen but constrained to those
/// present among the filter'd permanents.
/// </para>
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the following
/// criteria: it doesn't require a target (see rule 115.6), it could add mana to a
/// player's mana pool when it resolves, and it's not a loyalty ability." The "legendary
/// creatures and planeswalkers you control" filter is not a target (CR 115.6); the
/// ability satisfies all three criteria.
/// </para>
///
/// <para>
/// Runs at Priority = 1040 — above
/// <see cref="AddManaForEachColorAmongControlledPermanentsEffectRule"/> (1030) and
/// <see cref="AddManaEffectRule"/> (1000) so the "any color among" shape wins before
/// the per-color loop or the unconstrained "any color" match. The regex is anchored
/// at both ends (^…$) so it cannot match as a substring of a more-specific sibling.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1040)]
public sealed class AddManaAnyColorAmongEffectRule : IActivatedEffectRule
{
  // Anchored regex matching:
  //   "Add one mana of any color among legendary creatures and planeswalkers you control."
  // "legendary" is required; "creatures and planeswalkers" is the only WotC-printed
  // variant for this mechanic. Anchored at both ends.
  private static readonly Regex AnyColorAmongLegendaryCreaturesAndPlaneswalkers = new(
    @"^Add\s+one\s+mana\s+of\s+any\s+color\s+among\s+legendary\s+creatures\s+and\s+planeswalkers\s+you\s+control\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = AnyColorAmongLegendaryCreaturesAndPlaneswalkers.Match(effectText.Trim());
    if (!match.Success)
      return null;

    // CR 305.6 / 306.4: planeswalkers are legendary by rule. The oracle text
    // "legendary creatures and planeswalkers" describes permanents that are either
    // legendary creatures OR planeswalkers (all of which are legendary).
    // Modelled as CardTypes: ["creature", "planeswalker"], Supertypes: ["Legendary"],
    // Controller: You, Zone: Battlefield — the set whose colors constrain the choice.
    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      AnyColorAmong = new ObjectFilter
      {
        CardTypes = ["creature", "planeswalker"],
        Supertypes = ["Legendary"],
        Controller = ControllerFilter.You,
        Zone = Zone.Battlefield,
      },
    };
  }
}
