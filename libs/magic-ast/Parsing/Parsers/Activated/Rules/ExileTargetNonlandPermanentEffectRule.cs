namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target nonland permanent." — moves a targeted nonland permanent from the
/// battlefield to the exile zone.
///
/// <para>
/// "Nonland" excludes land cards (CR 205.3a — land is a card type) from the legal
/// target set. Encoded as <see cref="ObjectFilter.ExcludedCardTypes"/> = <c>["land"]</c>
/// on a permanent filter, paralleling the nonland-permanent shape in
/// <see cref="SacrificeCostRule"/>.
/// </para>
///
/// <para>
/// ANCHORED (^…$): prevents matching inside a more-specific sibling such as a two-type
/// disjunction or a qualified exile. Priority 982 — just below
/// <see cref="ExileFromGraveyardEffectRule"/> (984) and above generic exile shapes so
/// this specific nonland-permanent qualifier is tried first.
/// </para>
///
/// <para>
/// CR 701.13a (verbatim): "To exile an object, move it to the exile zone from wherever
/// it is."
/// CR 205.3a: "The card types are artifact, battle, conspiracy, creature, dungeon,
/// enchantment, instant, land, phenomenon, plane, planeswalker, scheme, sorcery,
/// tribual, and vanguard."
/// </para>
///
/// <para>
/// Canonical card: Preston, the Vanisher (BLB) —
/// "{1}{W}, Sacrifice five Illusions: Exile target nonland permanent."
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 982)]
public sealed class ExileTargetNonlandPermanentEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Exile\s+target\s+nonland\s+permanent\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          ExcludedCardTypes = ["land"],
        },
      },
    };
  }
}
