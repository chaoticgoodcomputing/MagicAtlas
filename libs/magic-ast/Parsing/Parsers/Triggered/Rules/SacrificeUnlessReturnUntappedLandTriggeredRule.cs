namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it unless you return an untapped [basic land type] you control to its
/// owner's hand" — the Visions "Karoo" bounce-land ETB pattern (Everglades, Karoo,
/// Coral Atoll, Dormant Volcano, Jungle Basin). Rule 701.21a — Sacrifice; the "unless"
/// clause is a preventive alternative cost (CR 117.7-adjacent — paying a cost to avoid
/// an effect), not a separate effect.
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "When this land enters"
///   effect  = "sacrifice it unless you return an untapped Swamp you control to its
///              owner's hand"
/// </para>
///
/// <para>
/// "it" refers back to the land named as the trigger subject — the pronoun-reference
/// convention shared with <see cref="SacrificeUnlessPayTriggeredRule"/> (Karoo/ETB
/// self-sacrifice uses <see cref="ObjectReferenceKind.It"/>, not Self). "untapped" is a
/// tapped/untapped status (CR 110.5) modeled as a <c>TappedStateCharacteristic { Tapped =
/// false }</c>. "Swamp" is a basic land subtype (CR 305.6); "its owner's hand" is by
/// ownership (CR 108.3) — modeled the same way as the cost-position return in
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.ReturnLandSubtypeToHandCostRule"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeUnlessReturnUntappedLandTriggeredRule : ITriggeredRule
{
  // Anchored: only matches the exact "sacrifice it unless you return an untapped
  // <subtype> you control to its owner's hand" shape. <subtype> is validated against
  // the basic land types below to avoid over-matching an unrelated word.
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+unless\s+you\s+return\s+an?\s+untapped\s+(?<subtype>[A-Za-z]+)\s+you\s+control\s+to\s+its\s+owner's\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Basic land subtypes (CR 305.6). Only these may appear as an "untapped <subtype>"
  // return-cost object in this pattern — an unrecognized word means this rule should
  // not claim the text.
  private static readonly HashSet<string> BasicLandSubtypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "Plains", "Island", "Swamp", "Mountain", "Forest",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var subtype = m.Groups["subtype"].Value;
    if (!BasicLandSubtypes.Contains(subtype))
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      new SacrificeEffect { Target = ObjectReference.It() },
      new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = new ReturnToHandCost
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Any,
            Filter = new ObjectFilter
            {
              CardTypes = ["land"],
              Subtypes = [subtype],
              Controller = ControllerFilter.You,
              Characteristics = [new TappedStateCharacteristic { Tapped = false }],
            },
          },
        },
      });
    return true;
  }
}
