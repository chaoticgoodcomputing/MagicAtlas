namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When you control no lands, [effect]." — a state-triggered ability
/// (CR 603.8 — "Some triggered abilities trigger when a game state (such as a
/// player controlling no permanents of a particular card type) is true", i.e. a
/// state trigger, NOT the CR 603.2e "becomes"-transition trigger).
/// Fires when the controlling player transitions to controlling zero lands of
/// any kind (no basic-land-subtype qualifier), distinct from
/// <see cref="ControlNoLandTypeConditionRule"/>, which matches the Islandhome-style
/// "when you control no [Islands|Forests|Swamps|Mountains|Plains]" subtype-scoped
/// form. Both share <see cref="TriggerEvent.ControlNoLandType"/> — the underlying
/// state-trigger shape is identical; the only structural difference is whether the
/// Filter carries a <c>Subtypes</c> restriction. Here it does not: the filter is
/// simply "a land" (CardTypes=["land"], Controller=You), matching the "a land" /
/// "another land" filter convention used elsewhere for the unqualified land type
/// (<see cref="TriggeredRuleHelpers.ParseObjectFilter"/>).
///
/// <para>
/// Example: Serendib Djinn (ARN): "When you control no lands, sacrifice this
/// creature."
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) on the literal "you control no lands" surface so this
/// cannot claim any subtype-qualified sibling ("you control no Islands", etc.) —
/// those retain the more specific <see cref="ControlNoLandTypeConditionRule"/>.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class ControlNoLandsConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^(?:When|Whenever)\s+you\s+control\s+no\s+lands\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(triggerText))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ControlNoLandType,
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Controller = ControllerFilter.You,
      },
    };
  }
}
