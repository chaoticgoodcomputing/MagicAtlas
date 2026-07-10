namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When enchanted land dies" — the Zendikon cycle's self-return trigger condition.
/// The generic sibling of <see cref="EnchantedBasicLandTypePutIntoGraveyardConditionRule"/>
/// (which is scoped to a single BASIC land subtype, e.g. "enchanted Swamp") and of the
/// generic "enchanted creature" branch of <see cref="TriggeredRuleHelpers.ParseObjectFilter"/>
/// (which <see cref="DiesConditionRule"/> uses) — that helper has no "enchanted land"/
/// "enchanted [type]" branch, so this dedicated rule is added rather than widening the
/// shared helper.
///
/// <para>
/// CR 700.4: "The term dies means 'is put into a graveyard from the battlefield.'"
/// CR 303.4c / 702.5: an Aura's "enchanted [type]" refers to the permanent it's
/// attached to (recorded on <see cref="ObjectFilter.IsEnchanted"/>, mirroring the
/// IsSelf/IsToken axes). The card type stays whatever noun the oracle text used
/// ("land" here), matching the Aura's own "Enchant land" restriction.
/// </para>
///
/// <para>
/// Canonical card: Vastwood Zendikon (and the rest of the Rise of the Eldrazi
/// Zendikon cycle) — "When enchanted land dies, return that card to its owner's
/// hand." The effect half ("return that card to its owner's hand") is handled
/// separately by <see cref="ReturnThatCardToHandOnDeathTriggeredRule"/>.
/// </para>
///
/// <para>
/// Priority 992 — above the generic <see cref="DiesConditionRule"/> (991), whose
/// <c>ParseObjectFilter</c> only recognises "enchanted creature" (not "enchanted
/// land") and would otherwise return null and leave the trigger unparsed. Anchored
/// pattern (whole trigger-condition string) prevents substring collisions with
/// longer/differently-scoped siblings.
/// </para>
///
/// Rule 603.2 (Triggered Abilities); Rule 700.4 ("dies"); Rule 303.4c / 702.5
/// ("enchanted [type]" refers to the Aura's attached permanent).
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class EnchantedTypeDiesConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^\s*(?:whenever|when|at)\s+enchanted\s+(?<type>land|artifact|creature|permanent|enchantment|planeswalker)\s+dies\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enchanted") || !lower.Contains("dies"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var type = match.Groups["type"].Value.ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        CardTypes = [type],
        IsEnchanted = true,
      },
    };
  }
}
