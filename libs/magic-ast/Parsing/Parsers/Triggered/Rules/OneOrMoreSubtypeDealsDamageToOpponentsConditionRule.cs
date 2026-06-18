namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever one or more [Subtype] you control deal damage to your opponents" —
/// the group-damage-to-multiple-opponents trigger condition (Malcolm, Keen-Eyed
/// Navigator pattern).
///
/// <para>
/// Emits <see cref="TriggerEvent.DealsDamageToOpponents"/> (plural). The Filter
/// carries the creature subtype and controller (You). Distinct from
/// <see cref="DealsDamageToOpponentConditionRule"/> (singular "an opponent" /
/// single source) and <see cref="DealsCombatDamageConditionRule"/> (combat damage
/// only).
/// </para>
///
/// <para>
/// Rule 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. This is generally detrimental to the object or player that receives
/// that damage." Rule 102.2: an opponent is any player not on the same team as
/// the controller. Rule 603.2: triggered abilities fire automatically on matching
/// game events.
/// </para>
///
/// <para>
/// ANCHORED (^…$): matched against the full trigger clause (after timing-word strip)
/// to prevent misfiring on broader trigger phrases that include this as a substring.
/// Priority 987: above <see cref="DealsDamageToOpponentConditionRule"/> (986) so the
/// more specific "your opponents" (plural) shape is tried before the singular form.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 987)]
public sealed class OneOrMoreSubtypeDealsDamageToOpponentsConditionRule : ITriggerConditionRule
{
  // "one or more [Subtype] you control deal damage to your opponents"
  // Subtype must be a proper-noun (capitalised first letter) to distinguish creature
  // subtypes ("Pirate", "Ally") from card-type words ("creature", "land").
  // Rule 205.3m: creature subtypes are capitalised in oracle text.
  private static readonly Regex _pattern = new(
    @"^one\s+or\s+more\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+deal\s+damage\s+to\s+your\s+opponents$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick guard before the heavier regex.
    if (!lower.Contains("one or more") || !lower.Contains("to your opponents"))
    {
      return null;
    }

    // Strip leading timing word so the anchored pattern matches the condition body.
    var body = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );

    var m = _pattern.Match(body);
    if (!m.Success)
    {
      return null;
    }

    var rawSubtype = m.Groups["subtype"].Value;
    // Normalise to capitalised form (Rule 205.3m).
    // Oracle text uses the plural form in "one or more Pirates you control" —
    // strip a trailing 's' to recover the canonical singular subtype name
    // (Rule 205.3m: creature subtypes are listed in their singular form on
    // type lines; oracle body text may use the plural for grammatical agreement).
    var singular = rawSubtype.EndsWith('s') && rawSubtype.Length > 2
      ? rawSubtype[..^1]
      : rawSubtype;
    var subtype = char.ToUpperInvariant(singular[0]) + singular[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsDamageToOpponents,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
