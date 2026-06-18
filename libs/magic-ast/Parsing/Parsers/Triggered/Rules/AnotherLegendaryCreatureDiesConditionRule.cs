namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another legendary creature you control dies" — the Ratadrabik of Urborg trigger.
/// Fires on <see cref="TriggerEvent.Dies"/> filtering for legendary creatures under
/// the controller's control, excluding the source permanent itself.
///
/// <para>
/// CR 702.20a: "Vigilance is a static ability…" — vigilance is the keyword context
/// for this card. CR 603.2: a triggered ability fires whenever its event occurs.
/// The filter narrows the match to legendary creatures you control (not the source).
/// </para>
///
/// <para>
/// Rule 205.4: "Legendary is a supertype. It is placed before the card's type."
/// Supertypes on <see cref="ObjectFilter.Supertypes"/> encode the positive-match
/// requirement (CR 205.4 — legendary must be among the permanent's supertypes).
/// </para>
///
/// <para>
/// The "another" qualifier (CR 109.5 — "another" excludes the named object itself)
/// maps to <see cref="ObjectFilter.ExcludeSelf"/> = true, consistent with the
/// DiesConditionRule and AnotherSubtypeEntersConditionRule conventions.
/// </para>
///
/// <para>
/// ANCHORED (^…$): the pattern targets a highly specific surface phrase and is fully
/// anchored to prevent matching inside broader sibling trigger texts. Priority 994
/// (above the generic DiesConditionRule at 991, below IsSelfByNameTrigger-based rules)
/// so this more-specific legendary filter is claimed first.
/// </para>
///
/// <para>
/// Rule citations: CR 603.2 (triggered ability), CR 205.4 (Legendary supertype),
/// CR 109.5 ("another" exclusion), CR 700.4 ("dies" → moved to graveyard from battlefield).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class AnotherLegendaryCreatureDiesConditionRule : ITriggerConditionRule
{
  // "another legendary creature you control dies"
  // Not anchored at start (trigger text includes the timing word "Whenever …").
  // Anchored at end so "dies" must be the final word — prevents spurious matches
  // where "dies" appears inside a compound trigger that has more text after it.
  private static readonly Regex _pattern = new(
    @"another\s+legendary\s+creature\s+you\s+control\s+dies$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("legendary") || !lower.Contains("dies"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Supertypes = ["Legendary"],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      },
    };
  }
}
