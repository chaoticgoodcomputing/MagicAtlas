namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Trigger condition: "a nontoken creature is put into your graveyard from the battlefield."
///
/// <para>
/// This is the long-form "dies" trigger (CR 700.4: "a creature, planeswalker, or battle is
/// put into a graveyard from the battlefield") qualified with two restrictions:
/// <list type="bullet">
///   <item><b>nontoken</b> — the creature is not a token (CR 111.1: a token is not a card;
///   the <c>IsToken = false</c> axis on <see cref="ObjectFilter"/> encodes this).</item>
///   <item><b>your graveyard</b> — the creature entered a graveyard you own
///   (<c>Controller = You</c> on the filter).</item>
/// </list>
/// Maps to <see cref="TriggerEvent.Dies"/> because "put into your graveyard from the
/// battlefield" is definitionally the Dies event (CR 700.4), with the controller constraint
/// naming whose graveyard receives it. The nontoken qualifier is the standard creature-token
/// distinction used throughout the corpus (CR 111.1).
/// </para>
///
/// <para>
/// Canonical card: Nim Deathmantle (SOM) — "Whenever a nontoken creature is put into your
/// graveyard from the battlefield, you may pay {4}. If you do, return that card to the
/// battlefield and attach this Equipment to it."
/// </para>
///
/// <para>
/// CR 700.4 (verbatim): "The word 'dies' means 'is put into a graveyard from the battlefield.'"
/// CR 111.1 (tokens are not cards).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class NontokenCreatureToGraveyardFromBattlefieldConditionRule : ITriggerConditionRule
{
  // Matches: "a nontoken creature is put into your graveyard from the battlefield"
  // The leading timing word (Whenever) is stripped by the dispatcher before this rule is called.
  private static readonly Regex _pattern = new(
    @"\ba\s+nontoken\s+creature\s+is\s+put\s+into\s+your\s+graveyard\s+from\s+the\s+battlefield\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(lower))
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
        IsToken = false,
        Controller = ControllerFilter.You,
      },
    };
  }
}
