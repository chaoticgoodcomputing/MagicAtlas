namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return all [Subtype] creature cards from your graveyard to the battlefield
/// [tapped]." — the subtype-qualified sibling of <see cref="ReturnAllFromGraveyardToBattlefieldRule"/>
/// (Knights' Charge: "Return all Knight creature cards from your graveyard to the
/// battlefield.").
///
/// <para><see cref="ReturnAllFromGraveyardToBattlefieldRule"/> intentionally bails on a
/// subtype-qualified type phrase ("Knight creature") because its captured type word
/// isn't a recognised base card type — writing it straight into <c>CardTypes</c> would
/// mislabel the filter. This rule owns that declined shape: the captured word is a
/// creature SUBTYPE (CR 205.3m), not a base card type, so it lands on
/// <see cref="ObjectFilter.Subtypes"/> with <c>CardTypes=["creature"]</c>.</para>
///
/// <para>CR 701 (zone change); CR 400.1 (zones); the optional "tapped" entering status
/// is CR 110.5b. Anchored (^…$) on "Return all" so it cannot consume a sibling
/// single-target return rule, and requires the "[Subtype] creature cards" shape
/// specifically so it cannot claim the bare-type sibling's surface.</para>
///
/// <para>Priority 992 — above <see cref="ReturnAllFromGraveyardToBattlefieldRule"/> (989)
/// so this more specific subtype form is tried first; that rule's own bail-out means
/// there's no real race, but the ordering documents the intent explicitly.</para>
/// </summary>
[ActivatedEffectRule(Priority = 992)]
public sealed class ReturnAllSubtypeCreatureFromGraveyardToBattlefieldRule : IActivatedEffectRule
{
  // Subtype must be a proper-noun (capitalised) word or two-word subtype (e.g. "Phyrexian
  // Germ") to distinguish creature subtypes from the base card-type nouns already owned
  // by ReturnAllFromGraveyardToBattlefieldRule.
  private static readonly Regex Pattern = new(
    @"^Return\s+all\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+creature\s+cards?\s+from\s+(?:your|the)\s+graveyard\s+to\s+the\s+battlefield(?:\s+(?<tapped>tapped))?$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];
    var tapped = m.Groups["tapped"].Success;

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
      Tapped = tapped,
    };
  }
}
