namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a [Subtype] you control is dealt damage" — a subtype-filtered damage-receipt
/// trigger keyed on the indefinite article "a"/"an" rather than "this". The
/// triggering object is some permanent you control sharing the named subtype (not
/// necessarily the ability's own source), so the filter carries the subtype on the
/// Subtypes axis with a You controller restriction, mirroring how
/// <see cref="SubtypeEntersConditionRule"/> handles "a [Subtype] you control
/// enters" for the Enters event.
///
/// <para>
/// Wrathful Red Dragon: "Whenever a Dragon you control is dealt damage, it deals
/// that much damage to any target that isn't a Dragon." The subject need not be
/// Wrathful Red Dragon itself — any Dragon you control qualifies — so the paired
/// effect resolves "it" as <see cref="ObjectReferenceKind.It"/> (the previously
/// mentioned triggering object), not <see cref="ObjectReferenceKind.Self"/>.
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players." CR 603.2: "Whenever a game event or game state matches a triggered
/// ability's trigger event, that ability automatically triggers." This is the
/// subtype-indefinite sibling of <see cref="CreatureDealtDamageConditionRule"/>
/// (generic "a creature is dealt damage") and
/// <see cref="SelfCreatureDealtDamageConditionRule"/> ("this creature is dealt
/// damage"); higher priority than both so the named-subtype reading wins whenever
/// the oracle text names a proper-noun subtype instead of the plain word
/// "creature".
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 700)]
public sealed class SubtypeDealtDamageConditionRule : ITriggerConditionRule
{
  // Matches "a|an <Subtype> you control is dealt damage". Subtype must be a
  // proper-noun (capitalised first letter) so plain card-type words ("creature")
  // are NOT captured here — those are handled by the generic siblings above.
  // Rule 205.3: subtypes are capitalised in oracle text; card-type words are
  // lowercase. NOT using IgnoreCase to preserve that casing distinction.
  private static readonly Regex _pattern = new(
    @"\ban?\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+is\s+dealt\s+damage\b",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("is dealt damage"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (oracle text capitalises subtypes — Rule 205.3).
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CreatureDealtDamage,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
