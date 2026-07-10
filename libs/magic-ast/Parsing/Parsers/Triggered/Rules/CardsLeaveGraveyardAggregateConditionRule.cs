namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever one or more cards leave your graveyard, ..." — the aggregate
/// (untyped) leave-graveyard trigger (Quintorius, Field Historian).
///
/// <para>
/// CR 603.2 (verbatim): "Whenever a game event or game state matches a
/// triggered ability's trigger event, that ability automatically triggers."
/// The "one or more" qualifier is a threshold condition on the trigger event,
/// recorded structurally on <see cref="TriggerCondition.MinimumCount"/> (=1) —
/// the same convention used by <see cref="SacrificeOneOrMoreSubtypeConditionRule"/>
/// for the analogous "you sacrifice one or more [Subtype]s" shape. The event
/// maps to <see cref="TriggerEvent.LeavesGraveyard"/>, which already exists for
/// the untyped-card "a card leaves your graveyard" concept (documented on the
/// enum member with Syr Konrad, the Grim as the singular-form precedent). The
/// filter carries the generic <c>CardTypes = ["card"]</c> (no type restriction —
/// "cards", not "creature cards" or "land cards") plus <c>Controller = You</c>
/// ("your graveyard").
/// </para>
///
/// <para>
/// Distinct from <see cref="SyrKonradTripleOrConditionRule"/>'s tertiary clause,
/// which is the SINGULAR "a creature card leaves your graveyard" (no "one or
/// more", type-restricted to creature) embedded inside a three-event compound
/// trigger — that rule's anchored pattern spans the full triple-or phrase and
/// cannot match this simpler, standalone shape (and vice versa).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 981)]
public sealed class CardsLeaveGraveyardAggregateConditionRule : ITriggerConditionRule
{
  // Anchored to the full trigger body (after the leading timing word is
  // stripped) so this cannot match as a substring inside a longer/compound
  // trigger clause handled by a more-specific sibling rule (e.g. Syr Konrad's
  // triple-or phrase, which also contains "leaves your graveyard").
  private static readonly Regex _pattern = new(
    @"^one\s+or\s+more\s+cards?\s+leave\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("leave") || !lower.Contains("graveyard") || !lower.Contains("one or more"))
    {
      return null;
    }

    // Strip the leading timing word ("whenever") before matching the body.
    var body = Regex.Replace(triggerText.Trim(), @"^whenever\s+", string.Empty, RegexOptions.IgnoreCase).Trim();

    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.LeavesGraveyard,
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        Controller = ControllerFilter.You,
      },
      MinimumCount = 1,
    };
  }
}
