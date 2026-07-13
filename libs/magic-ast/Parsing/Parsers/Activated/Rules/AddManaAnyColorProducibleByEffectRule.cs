namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "Add one mana of any color that a land an opponent controls could produce."
/// — the Exotic Orchard / Fellwar Stone shape. The produced mana is ONE unit of
/// a freely-chosen color, but the chosen color is constrained to the colors of
/// mana that lands the opponent controls <i>could produce</i> (their mana-
/// production capability), not the colors of those lands as objects.
///
/// <para>
/// CR 106.7 (verbatim, which names this exact card): "Some abilities produce mana
/// based on the type of mana another permanent or permanents 'could produce.' The
/// type of mana a permanent could produce at any time includes any type of mana
/// that an ability of that permanent would produce if the ability were to resolve
/// at that time… Example: Exotic Orchard has the ability '{T}: Add one mana of any
/// color that a land an opponent controls could produce.'"
/// </para>
///
/// <para>
/// Modelled with <see cref="AddManaEffect.AnyColorProducibleBy"/> carrying the
/// <see cref="ObjectFilter"/> for the constraining permanents
/// (<c>CardTypes=["land"], Controller=Opponent, Zone=Battlefield</c>), with
/// <see cref="AddManaEffect.Mana"/> left <c>""</c> because the color is chosen at
/// resolution. This is DISTINCT from
/// <see cref="AddManaAnyColorAmongEffectRule"/> (Mox Amber: color present among
/// the permanents' own colors) — a Plains produces {W} but is colorless as a
/// permanent, so "could produce" ≠ "color among".
/// </para>
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the
/// following criteria: it doesn't require a target (see rule 115.6), it could add
/// mana to a player's mana pool when it resolves, and it's not a loyalty ability."
/// The "a land an opponent controls" phrase is not a target (CR 115.6); the
/// enclosing "{T}: Add …" ability satisfies all three criteria, so it carries
/// <c>IsManaAbility = true</c>.
/// </para>
///
/// <para>
/// Runs at Priority = 1045 — above <see cref="AddManaEffectRule"/> (1000). The
/// regex is anchored at both ends (^…$) so it can only match the whole effect
/// string, never a substring of a more-specific sibling. The controller phrase is
/// captured so the naturally-related "a land you control could produce" variant of
/// this same mechanic maps to <see cref="ControllerFilter.You"/> without a second
/// rule; the "type" (colorless-inclusive) variant is intentionally out of scope.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1045)]
public sealed class AddManaAnyColorProducibleByEffectRule : IActivatedEffectRule
{
  private static readonly Regex AnyColorProducibleBy = new(
    @"^Add\s+one\s+mana\s+of\s+any\s+color\s+that\s+a\s+land\s+(?<who>an\s+opponent\s+controls|you\s+control)\s+could\s+produce\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = AnyColorProducibleBy.Match(effectText.Trim());
    if (!match.Success)
      return null;

    var controller = match.Groups["who"].Value.Contains("opponent")
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new AddManaEffect
    {
      Mana = string.Empty,
      AnyColor = false,
      AnyColorProducibleBy = new ObjectFilter
      {
        CardTypes = ["land"],
        Controller = controller,
        Zone = Zone.Battlefield,
      },
    };
  }
}
