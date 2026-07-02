namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Destroy target permanent that's one or more colors." — destroys a colored
/// (non-colorless) permanent of any type.
///
/// <para>
/// "One or more colors" means the permanent has at least one of the five Magic
/// colors (CR 105.1: "There are five colors in the Magic game: white, blue, black,
/// red, and green. Colorless is not a color."). This is the negation of colorless —
/// any permanent that has a color satisfies this filter. The characteristic
/// "one or more colors" is encoded as an <see cref="OtherCharacteristic"/> residual
/// since <see cref="ObjectFilter"/> does not yet have a structured <c>IsColored</c>
/// axis (the complement of <see cref="ObjectFilter.IsColorless"/>). It is a
/// meaningful, rules-distinct filter — not arbitrary free text — and is a candidate
/// for a future first-class boolean axis on <c>ObjectFilter</c>.
/// </para>
///
/// <para>
/// Fully anchored (^…$). GUARD: handles only "Destroy target permanent that's one
/// or more colors." Does NOT handle "Destroy target [type]" (sibling
/// <see cref="DestroyTargetEffectRule"/>), "Destroy target multicolored permanent"
/// (spell-parser family), or "Destroy target nonland permanent" (different filter axis).
/// Priority 599 — one below <see cref="DestroyTargetEffectRule"/> (600) so the
/// bare-type destroy rule is tried first.
/// </para>
///
/// <para>CR 701.8a (destroy); CR 105.1 (colors).</para>
/// </summary>
[ActivatedEffectRule(Priority = 599)]
public sealed class DestroyColoredPermanentEffectRule : IActivatedEffectRule
{
  // The right single quotation mark in "that's" may be either the plain apostrophe
  // (U+0027) or the curly right single quotation mark (U+2019) used in Scryfall
  // oracle text — both are matched by the character class [’'].
  private static readonly Regex _pattern = new(
    "^\\s*Destroy\\s+target\\s+permanent\\s+that[’']s\\s+one\\s+or\\s+more\\s+colors\\.?\\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
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
          CardTypes = ["permanent"],
          Characteristics = [Characteristic.Other("one or more colors")],
        },
      },
    };
  }
}
