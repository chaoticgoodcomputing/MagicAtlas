namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a [Subtype] you control becomes tapped, ..." — a subtype-filtered
/// "becomes tapped" trigger keyed on the indefinite article "a"/"an" (mirrors
/// <see cref="SubtypeEntersConditionRule"/>'s "a [Subtype] you control enters"
/// shape, but for the <see cref="TriggerEvent.BecomesTapped"/> event instead of
/// <see cref="TriggerEvent.Enters"/>). The triggering object is some OTHER
/// permanent (not necessarily this card) that shares the named creature subtype
/// and is controlled by this permanent's controller.
///
/// CR 603.2: "Some trigger events use the word 'becomes' (for example, 'becomes
/// attached' or 'becomes blocked'). These trigger only at the time the named
/// event happens... An ability that triggers when a permanent 'becomes tapped'
/// or 'becomes untapped' doesn't trigger if the permanent enters the
/// battlefield in that state."
///
/// Example: "Whenever a Dwarf you control becomes tapped, create a Treasure
/// token." (Magda, Brazen Outlaw).
///
/// Sits just below the generic <see cref="BecomesTappedConditionRule"/>
/// (Priority 985) so the plain card-type shapes ("this creature becomes
/// tapped", "a creature you control becomes tapped") get first refusal; this
/// rule only fires once that generic recogniser has declined, then narrows on
/// the proper-noun subtype word (which the generic recogniser's
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> has no branch for).
/// </summary>
[TriggerConditionRule(Priority = 984)]
public sealed class SubtypeBecomesTappedConditionRule : ITriggerConditionRule
{
  // Matches "a|an <Subtype> you control becomes tapped". Subtype must be a
  // proper-noun (capitalised first letter) so type words ("creature", "land",
  // "artifact") are NOT captured here — those plain shapes are handled by
  // BecomesTappedConditionRule's ParseObjectFilter. Rule 205.3: subtypes are
  // capitalised in oracle text; card-type words are lowercase. NOT using
  // IgnoreCase to preserve that casing distinction.
  private static readonly Regex _pattern = new(
    @"\ban?\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+becomes\s+tapped",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes tapped"))
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
      Event = TriggerEvent.BecomesTapped,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
