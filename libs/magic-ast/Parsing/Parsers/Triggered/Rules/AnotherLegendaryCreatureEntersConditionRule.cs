namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another legendary creature you control enters" — the Gimli of the Glittering
/// Caves trigger. Fires on <see cref="TriggerEvent.Enters"/> filtering for legendary
/// creatures under the controller's control, excluding the source permanent itself.
///
/// <para>
/// This is the "enters" analogue of <see cref="AnotherLegendaryCreatureDiesConditionRule"/>
/// (which handles the same subject filter for the Dies event). CR 603.6a: "Enters-the-
/// battlefield abilities trigger when a permanent enters the battlefield." CR 603.2:
/// "Whenever a game event or game state matches a triggered ability's trigger event,
/// that ability automatically triggers." The filter narrows the match to legendary
/// creatures you control (not the source).
/// </para>
///
/// <para>
/// Rule 205.4: "Legendary is a supertype. It is placed before the card's type."
/// Supertypes on <see cref="ObjectFilter.Supertypes"/> encode the positive-match
/// requirement (CR 205.4 — legendary must be among the permanent's supertypes).
/// </para>
///
/// <para>
/// The "another" qualifier ("another" = any object other than this source; plain-language
/// English, no dedicated CR rule number in the bundled data) maps to
/// <see cref="ObjectFilter.ExcludeSelf"/> = true, consistent with the
/// EntersConditionRule and AnotherLegendaryCreatureDiesConditionRule conventions.
/// </para>
///
/// <para>
/// ANCHORED (end): the pattern targets a highly specific surface phrase; "enters" must
/// be the final word so this does not match inside a broader compound trigger clause.
/// Priority 994 (mirrors AnotherLegendaryCreatureDiesConditionRule) — above the generic
/// EntersConditionRule (990), which would otherwise fail on this shape anyway because
/// ParseObjectFilter has no branch for the "legendary creature" supertype-qualified noun
/// phrase (it would return null and EntersConditionRule would decline to match).
/// </para>
///
/// <para>
/// Rule citations: CR 603.2 (triggered ability), CR 603.6a (enters-the-battlefield
/// abilities), CR 205.4 (Legendary supertype). ("another" is plain-language exclusion —
/// no dedicated CR rule — modeled on the ExcludeSelf axis.)
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class AnotherLegendaryCreatureEntersConditionRule : ITriggerConditionRule
{
  // "another legendary creature you control enters"
  // Not anchored at start (trigger text includes the timing word "Whenever …").
  // Anchored at end so "enters" must be the final word — prevents spurious matches
  // where "enters" appears inside a compound trigger that has more text after it.
  private static readonly Regex _pattern = new(
    @"another\s+legendary\s+creature\s+you\s+control\s+enters$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("legendary") || !lower.Contains("enters"))
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
      Event = TriggerEvent.Enters,
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
