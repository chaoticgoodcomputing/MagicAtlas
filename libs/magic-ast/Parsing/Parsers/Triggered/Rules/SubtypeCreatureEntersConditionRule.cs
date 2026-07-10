namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a [Subtype] creature enters" — an unqualified creature-subtype ETB trigger,
/// with no "you control"/"another" qualifier, so it watches ANY entering creature
/// of the named subtype regardless of controller (Rule 603.2: a game event
/// matching the trigger event triggers the ability).
///
/// <para>
/// Paradigm card: Cloak and Dagger — "Whenever a Rogue creature enters, you may
/// attach this Equipment to it." The trigger doesn't restrict to creatures "you
/// control", so any Rogue entering (yours or an opponent's) fires it.
/// </para>
///
/// <para>
/// Distinct from <see cref="SubtypeEntersConditionRule"/> (requires "you control"
/// and omits the "creature" type word — e.g. "a Cartouche you control enters")
/// and from <see cref="AnotherSubtypeEntersConditionRule"/> (requires "another" +
/// "you control"). Here both the card-type word "creature" and the subtype are
/// present, and there's no controller qualifier at all.
/// </para>
///
/// <para>
/// Runs at priority 991 — above the generic <see cref="EntersConditionRule"/> (990),
/// whose <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> only recognises the
/// bare "a creature" substring and would otherwise silently drop the subtype
/// (since "a Rogue creature" does not contain the literal substring "a creature").
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 991)]
public sealed class SubtypeCreatureEntersConditionRule : ITriggerConditionRule
{
  // Matches "a|an <Subtype> creature enters[[ the battlefield]]", with no
  // "you control"/"another" qualifier present anywhere in the trigger text.
  // Subtype must be a proper-noun (capitalised first letter) so type words
  // ("creature", "artifact", ...) aren't captured — Rule 205.3m subtypes are
  // capitalised in oracle text. NOT using IgnoreCase to preserve that casing
  // distinction.
  private static readonly Regex _pattern = new(
    @"\ban?\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+creature\s+enters(?:\s+the\s+battlefield)?\s*$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("creature") || !lower.Contains("enters"))
    {
      return null;
    }

    // "you control" / "another" narrow the subject to a different (already-handled)
    // shape — decline so those more specific rules keep ownership of their surfaces.
    if (lower.Contains("you control") || lower.Contains("another"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (oracle text capitalises subtypes — Rule 205.3m).
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter { CardTypes = ["creature"], Subtypes = [subtype] },
    };
  }
}
