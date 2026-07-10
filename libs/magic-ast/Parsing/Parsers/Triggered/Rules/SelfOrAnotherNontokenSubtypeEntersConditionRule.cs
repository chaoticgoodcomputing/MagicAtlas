namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "[CardName] or another nontoken [Subtype] you control enters" — a self-by-name
/// disjunction enters trigger on a creature subtype. The oracle pattern
/// "X or another nontoken [Subtype] you control enters" fires when ANY nontoken
/// permanent of the named creature subtype the controller controls enters —
/// INCLUDING the source card itself, which is named in the "X" half of the
/// disjunction. Because the source's own entry triggers the ability, the filter
/// must be SELF-INCLUSIVE: NO <see cref="ObjectFilter.ExcludeSelf"/> is set.
///
/// <para>
/// This is the enters/subtype analogue of the dies-side disjunction guard in
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> (Anax, Hardened in the
/// Forge — "Whenever Anax or another nontoken creature you control dies") and of
/// <see cref="SelfOrAnotherArtifactEntersConditionRule"/> (Gonti's Aether Heart —
/// "Whenever Gonti's Aether Heart or another artifact you control enters"). All
/// three share the same rule: a self-name disjunction is self-inclusive, so the
/// bare-"another" ExcludeSelf convention (CR 109.5) does NOT apply here.
/// </para>
///
/// <para>
/// Paradigm card: Arahbo, the First Fang — "Whenever Arahbo or another nontoken
/// Cat you control enters, create a 1/1 white Cat creature token." Arahbo's own
/// entry triggers the ability.
/// </para>
///
/// <para>
/// The filter is CardTypes=creature + Subtypes=[Subtype] + IsToken=false +
/// Controller=You (CR 111: tokens are not cards, so "nontoken" restricts to
/// IsToken=false; Rule 205.3m: the capitalised word is a creature subtype).
/// CR 603.1: triggered abilities use "when"/"whenever"/"at" to watch for events.
/// CR 603.2: the trigger fires each time the condition is met.
/// </para>
///
/// <para>
/// ANCHOR: the pattern is anchored (^...$) and requires the literal
/// "or another nontoken [Capitalised Subtype] you control enters" tail — the
/// leading ".+" absorbs the timing keyword and the self-name half. A plain
/// "X or another nontoken creature you control enters" (lowercase "creature", no
/// proper-noun subtype) is NOT matched here — the subtype capture requires a
/// capital initial — and is instead handled by <see cref="EntersConditionRule"/>
/// via ParseObjectFilter's own nontoken-creature disjunction guard, so this rule
/// does not collide with that shape. Runs at Priority 996 — above the generic
/// <see cref="EntersConditionRule"/> (990) and the subtype-enters rules so the
/// more specific self-inclusive-nontoken shape wins.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 996)]
public sealed class SelfOrAnotherNontokenSubtypeEntersConditionRule : ITriggerConditionRule
{
  // Matches "[name words] or another nontoken [Subtype] you control enters[[ the battlefield]]".
  // Subtype must be a proper-noun (capitalised first letter) so type words
  // ("creature") aren't captured — Rule 205.3m: creature subtypes are capitalised
  // in oracle text. NOT using IgnoreCase so the casing distinction is preserved;
  // the fixed lowercase words ("or another nontoken", "you control enters") match
  // the oracle convention exactly.
  private static readonly Regex _pattern = new(
    @"^.+\s+or\s+another\s+nontoken\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+enters(?:\s+the\s+battlefield)?$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("or another nontoken") || !lower.Contains("enters"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (oracle text capitalises creature subtypes — Rule 205.3m).
    var subtype = char.ToUpperInvariant(rawSubtype[0]) + rawSubtype[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        IsToken = false,
        Controller = ControllerFilter.You,
        // No ExcludeSelf: the "X or another nontoken [Subtype]" disjunction
        // explicitly includes the source card itself (named in the "X" half).
      },
    };
  }
}
