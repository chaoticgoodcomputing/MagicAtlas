namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// S7 — "{mana} for each [Subtype] you control" (Elvish Archdruid and similar).
/// Counts only permanents of the named creature subtype controlled by the ability's
/// controller — a controller-scoped variant of S6 (Priest of Titania's "on the
/// battlefield", which counts ALL permanents of the subtype regardless of controller).
///
/// <para>
/// CR 605.1a: "An activated ability is a mana ability if it meets all of the
/// following criteria: it doesn't require a target (see rule 115.6), it could add
/// mana to a player's mana pool when it resolves, and it's not a loyalty ability." —
/// the enclosing "{T}: Add … you control" ability is a mana ability; the controller
/// filter does not introduce a target.
/// </para>
///
/// <para>
/// Runs at Priority = 1010 (higher than <see cref="AddManaEffectRule"/>'s 1000) so
/// the more-specific "you control" shape wins before the general bail on "for each"
/// in <see cref="AddManaEffectRule"/> is reached.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 1010)]
public sealed class AddManaForEachControlledSubtypeEffectRule : IActivatedEffectRule
{
  // "{G} for each Elf you control." — uppercase-initial creature subtype,
  // controller clause "you control", with optional trailing period.
  private static readonly Regex ForEachControlledSubtype = new(
    @"^Add\s+(?<mana>(?:\{[^}]+\})+)\s+for\s+each\s+(?<subtype>[A-Z][a-zA-Z'-]+)\s+you\s+control\.?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = ForEachControlledSubtype.Match(effectText.Trim());
    if (!match.Success)
      return null;

    var mana = match.Groups["mana"].Value;
    var subtype = match.Groups["subtype"].Value;

    return new AddManaEffect
    {
      Mana = mana,
      AnyColor = false,
      Amount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
          Zone = Zone.Battlefield,
        },
      },
    };
  }
}
