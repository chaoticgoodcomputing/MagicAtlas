namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a [Subtype] you control deals combat damage to a player" — the
/// subtype-subject variant of the deals-combat-damage-to-a-player trigger
/// (Ingenious Infiltrator: "Whenever a Ninja you control deals combat damage
/// to a player, …"). The subject is any creature of the named subtype under
/// the controller's control, so the Filter carries
/// <see cref="ObjectFilter.Subtypes"/> = [subtype] and
/// <see cref="ObjectFilter.Controller"/> = You.
///
/// <para>
/// Sits ABOVE <see cref="DealsCombatDamageConditionRule"/> (Priority 985) so the
/// subtype constraint is captured before that rule's generic
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> path is tried (which has no
/// "a &lt;Subtype&gt; you control" branch and would fall through to null). Mirrors
/// <see cref="SubtypeDealsCombatDamageToCreatureConditionRule"/>'s subtype-capture
/// convention (Rule 205.3m — creature subtypes are capitalised in oracle text) and
/// <see cref="OneOrMoreSubtypeDealsDamageToOpponentsConditionRule"/>'s "you control"
/// possessive handling, but for the singular "a &lt;Subtype&gt;" subject with the
/// "deals" (not "deal") verb form and the "to a player" recipient.
/// </para>
///
/// <para>
/// ANCHORED (^…$) against the full trigger clause (after the timing-word strip) so
/// this rule matches ONLY the exact "a &lt;Subtype&gt; you control deals combat
/// damage to a player" surface — case-sensitive on the subtype (leading capital)
/// so "a creature you control deals combat damage to a player" does NOT match here
/// and instead falls through to <see cref="DealsCombatDamageConditionRule"/>.
/// </para>
///
/// <para>
/// CR 510 (Combat Damage Step): combat damage assignment is the game event.
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class SubtypeYouControlDealsCombatDamageToPlayerConditionRule : ITriggerConditionRule
{
  // "a/an <Subtype> you control deals combat damage to a player". Subtype is
  // case-sensitive (leading capital, Rule 205.3m) so lowercase type words
  // ("creature") never match here.
  private static readonly Regex _pattern = new(
    @"^an?\s+(?<subtype>[A-Z][A-Za-z]+(?:\s+[A-Z][A-Za-z]+)?)\s+you\s+control\s+deals\s+combat\s+damage\s+to\s+a\s+player$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("deals combat damage") || !lower.Contains("to a player"))
    {
      return null;
    }

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

    var raw = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(raw[0]) + raw[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToPlayer,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
