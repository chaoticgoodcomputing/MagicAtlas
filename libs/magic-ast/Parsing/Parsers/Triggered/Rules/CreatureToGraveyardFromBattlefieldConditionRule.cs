namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Trigger condition: "a creature is put into your graveyard from the battlefield."
///
/// <para>
/// This is the long-form "dies" trigger (CR 700.4: "The term dies means 'is put
/// into a graveyard from the battlefield.'") qualified with a controller
/// restriction: <b>your graveyard</b> — the creature entered a graveyard you own
/// (<c>Controller = You</c> on the filter). Because a card is always put into
/// its OWNER's graveyard when it dies, naming "your graveyard" (rather than the
/// bare "a graveyard") restricts the trigger to creatures YOU own, distinct from
/// the unqualified <see cref="DiesConditionRule"/> ("a creature dies" / "is put
/// into a graveyard from the battlefield") which fires for ANY creature
/// regardless of whose graveyard receives it.
/// </para>
///
/// <para>
/// Sibling of <see cref="NontokenCreatureToGraveyardFromBattlefieldConditionRule"/>
/// (Nim Deathmantle's "a NONTOKEN creature is put into your graveyard..."): that
/// rule additionally excludes tokens (CR 111.1); this rule has no such
/// restriction. The two regexes are mutually exclusive by text — "nontoken" sits
/// between "a" and "creature" in the sibling's pattern, so this pattern's
/// `a\s+creature` never matches that sibling's surface.
/// </para>
///
/// <para>
/// Canonical card: Mortuary (VIS) — "Whenever a creature is put into your
/// graveyard from the battlefield, put that card on top of your library."
/// </para>
///
/// <para>
/// CR 700.4 (verbatim): "The term dies means 'is put into a graveyard from the
/// battlefield.'"
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class CreatureToGraveyardFromBattlefieldConditionRule : ITriggerConditionRule
{
  // Matches: "a creature is put into your graveyard from the battlefield", after
  // stripping the leading timing word (When/Whenever/At) that still prefixes
  // triggerText at this point in the dispatcher — mirrors the strip-then-anchor
  // convention used by other anchored ITriggerConditionRule implementations
  // (e.g. AttacksOrBlocksConditionRule). The required \s+ between "a" and
  // "creature" ensures this never matches the "nontoken" sibling's surface
  // ("a nontoken creature is put into your graveyard...").
  private static readonly Regex _leadingTiming = new(
    @"^(When|Whenever|At)\s+",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _pattern = new(
    @"^a\s+creature\s+is\s+put\s+into\s+your\s+graveyard\s+from\s+the\s+battlefield$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var stripped = _leadingTiming.Replace(triggerText.Trim(), string.Empty);
    if (!_pattern.IsMatch(stripped))
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
        Controller = ControllerFilter.You,
      },
    };
  }
}
