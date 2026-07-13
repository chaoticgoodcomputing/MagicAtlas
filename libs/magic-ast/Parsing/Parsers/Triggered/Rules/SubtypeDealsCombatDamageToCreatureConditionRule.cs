namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a [Subtype] deals combat damage to a creature" — the subtype-subject
/// variant of the deals-combat-damage-to-a-creature trigger (Toxin Sliver:
/// "Whenever a Sliver deals combat damage to a creature, …"). The subject is any
/// creature of the named subtype, so the Filter carries
/// <see cref="ObjectFilter.Subtypes"/> = [subtype].
///
/// <para>
/// Sits ABOVE <see cref="DealsCombatDamageToCreatureConditionRule"/> (Priority 986),
/// whose <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> path would otherwise
/// grab the recipient "a creature" and silently drop the subtype constraint. This
/// rule is anchored to the "a &lt;ProperNounSubtype&gt; deals combat damage to a
/// creature" surface (case-sensitive on the subtype so lowercase type words like
/// "creature" cannot match), mirroring
/// <see cref="AnotherSubtypeEntersConditionRule"/>'s subtype-capture convention
/// (Rule 205.3m — creature subtypes are capitalised in oracle text). Self-by-name
/// subjects ("Whenever Phage deals combat damage …") carry no "a"/"an" article and
/// so fall through to the self-by-name path unchanged.
/// </para>
///
/// <para>
/// CR 510.1 (Combat Damage Step): combat damage assignment is the game event.
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." The recipient class (a
/// creature) is implied by <see cref="TriggerEvent.DealsCombatDamageToCreature"/>.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class SubtypeDealsCombatDamageToCreatureConditionRule : ITriggerConditionRule
{
  // "a/an <Subtype> deals combat damage to a creature". Case-sensitive on the
  // subtype (a leading capital) so "a creature deals combat damage to a creature"
  // does NOT match here. Anchored to end so the recipient is exactly "a creature".
  private static readonly Regex _pattern = new(
    @"\ban?\s+(?<subtype>[A-Z][A-Za-z]+)\s+deals\s+combat\s+damage\s+to\s+a\s+creature$",
    RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("deals combat damage") || !lower.Contains("to a creature"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var raw = m.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(raw[0]) + raw[1..];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToCreature,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
      },
    };
  }
}
