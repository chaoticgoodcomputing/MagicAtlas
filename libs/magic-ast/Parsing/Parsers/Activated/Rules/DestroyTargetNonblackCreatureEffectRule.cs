namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target nonblack creature." — single-target destroy of a nonblack
/// creature as an activated-ability effect (Attrition: "{B}, Sacrifice a creature:
/// Destroy target nonblack creature.").
///
/// The "nonblack" qualifier is modeled on the structured colour-negation axis rather
/// than as free text: a nonblack creature is a creature without the color black
/// (CR 105.1: "There are five colors in the Magic game: white, blue, black, red, and
/// green."). Hence <c>CardTypes=["creature"]</c> + <c>ExcludedColors=["B"]</c>,
/// parallel to how the identical phrase is encoded on the spell side (Doom Blade —
/// see <see cref="ObjectFilter.ExcludedColors"/>) and to how "nonbasic land" is
/// encoded on this same activated-effect track (see
/// <see cref="DestroyTargetNonbasicLandEffectRule"/>).
///
/// CR 701.8a: "To destroy a permanent, move it from the battlefield to its owner's
/// graveyard."
///
/// GUARD: anchored to exactly "Destroy target nonblack creature". The bare-type
/// sibling <see cref="DestroyTargetEffectRule"/> (Priority 600) matches "Destroy
/// target creature" but NOT "nonblack creature" (its type group has no "nonblack
/// creature" alternative), so the two rules are disjoint; this rule sits at a higher
/// priority only to make precedence explicit, mirroring
/// <see cref="DestroyTargetNonbasicLandEffectRule"/>.
/// </summary>
[ActivatedEffectRule(Priority = 610)]
public sealed class DestroyTargetNonblackCreatureEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Destroy\s+target\s+nonblack\s+creature\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    if (!Pattern.IsMatch(effectText.Trim()))
    {
      return null;
    }

    return new DestroyEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          ExcludedColors = ["B"],
        },
      },
    };
  }
}
