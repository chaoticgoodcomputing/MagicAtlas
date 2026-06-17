namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// S8 — "Add {mana} for each creature you control." (Circle of Dreams Druid and similar).
/// Counts every creature the ability's controller controls on the battlefield — no subtype
/// restriction, card-type filter only.
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the following
/// criteria: it doesn't require a target (see rule 115.6), it could add mana to a player's
/// mana pool when it resolves, and it's not a loyalty ability." — the enclosing
/// "{T}: Add … you control" ability is a mana ability; the controller filter does not
/// introduce a target.
/// </para>
///
/// <para>
/// CR 122/quantity 'for each': the count is a <see cref="CountQuantity"/> over an
/// <see cref="ObjectFilter"/> with <c>CardTypes=["creature"]</c>,
/// <c>Controller=<see cref="ControllerFilter.You"/></c>,
/// <c>Zone=<see cref="Zone.Battlefield"/></c>.
/// </para>
///
/// <para>
/// Runs at Priority = 1020 (higher than
/// <see cref="AddManaForEachControlledSubtypeEffectRule"/>'s 1010) so the card-type
/// "creature" shape is tried before the subtype match — the regex is anchored at both
/// ends so there is no substring-overlap risk with the subtype rule.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1020)]
public sealed class AddManaForEachControlledCreatureEffectRule : IActivatedEffectRule
{
  // "Add {G} for each creature you control." — literal word "creature" (lowercase),
  // controller clause "you control", with optional trailing period.
  // Anchored at both ends so it cannot match "for each Elf creature you control"
  // (which also contains "creature") — the subtype-rule handles that shape.
  private static readonly Regex ForEachControlledCreature = new(
    @"^Add\s+(?<mana>(?:\{[^}]+\})+)\s+for\s+each\s+creature\s+you\s+control\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = ForEachControlledCreature.Match(effectText.Trim());
    if (!match.Success)
      return null;

    var mana = match.Groups["mana"].Value;

    return new AddManaEffect
    {
      Mana = mana,
      AnyColor = false,
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          Zone = Zone.Battlefield,
        },
      },
    };
  }
}
