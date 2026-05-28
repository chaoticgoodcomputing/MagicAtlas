namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a [Subtype] you control enters" — a subtype-filtered enters trigger keyed on
/// the indefinite article "a"/"an" rather than "another". The triggering object
/// is some OTHER permanent (not this card) that shares the named subtype; the
/// subtype is the most precise type word the oracle text gives, so the filter
/// carries it on the Subtypes axis with no CardTypes constraint.
///
/// Rule 603.6a: enters-the-battlefield abilities trigger when a permanent enters
/// the battlefield, written "When [this object] enters, ..." or
/// "Whenever a [type] enters, ...". Rule 603.2: when a game event matches a
/// triggered ability's trigger event, that ability automatically triggers.
///
/// This is distinct from a this-object ETB trigger (handled by
/// <see cref="EntersConditionRule"/> via the "this [type]" self-reference) and
/// from the creature-subtype "another [Subtype] you control enters" shape
/// (<see cref="AnotherSubtypeEntersConditionRule"/>, which keys on "another"
/// and pins CardTypes to creature). Here the subtype may be any permanent
/// subtype — e.g. the Aura subtype Cartouche — so no card-type is asserted.
///
/// Examples:
///   "When a Cartouche you control enters, ..."
///
/// Sits just below <see cref="EntersConditionRule"/> (Priority 990) so the
/// generic enters recogniser gets first refusal on plainer shapes
/// ("a creature you control enters", "this enchantment enters"); this rule only
/// fires once that generic recogniser has declined, then narrows on the proper-
/// noun subtype.
/// </summary>
[TriggerConditionRule(Priority = 989)]
public sealed class SubtypeEntersConditionRule : ITriggerConditionRule
{
  // Matches "a|an <Subtype> you control enters[[ the battlefield]]".
  // Subtype must be a proper-noun (capitalised first letter) so type words
  // ("creature", "land", "enchantment") are NOT captured — those plain shapes
  // are handled by EntersConditionRule's ParseObjectFilter. Rule 205.3:
  // subtypes are capitalised in oracle text; card-type words are lowercase.
  // NOT using IgnoreCase to preserve that casing distinction.
  private static readonly Regex _pattern = new(
    @"\ban?\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+enters",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters"))
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
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
