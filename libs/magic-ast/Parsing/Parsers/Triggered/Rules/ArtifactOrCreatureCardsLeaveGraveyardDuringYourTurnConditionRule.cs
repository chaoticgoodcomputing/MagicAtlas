namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever one or more artifact and/or creature cards leave your
/// graveyard during your turn, ..." (Thran Vigil) — a type-restricted,
/// turn-scoped sibling of the aggregate (untyped) leave-graveyard trigger
/// handled by <see cref="CardsLeaveGraveyardAggregateConditionRule"/>
/// (Quintorius, Field Historian: "Whenever one or more cards leave your
/// graveyard, ...").
///
/// <para>
/// CR 603.2 (verbatim): "Whenever a game event or game state matches a
/// triggered ability's trigger event, that ability automatically triggers."
/// The "one or more" qualifier is recorded structurally on
/// <see cref="TriggerCondition.MinimumCount"/> (=1), matching
/// <see cref="CardsLeaveGraveyardAggregateConditionRule"/>'s convention. The
/// event maps to the existing <see cref="TriggerEvent.LeavesGraveyard"/>. The
/// "artifact and/or creature" qualifier is a disjunctive type filter —
/// <see cref="ObjectFilter.CardTypes"/> = ["artifact", "creature"] — the same
/// multi-element-list-as-disjunction convention used for "an artifact or
/// creature" on Panharmonicon. The trailing "during your turn" qualifier is
/// carried on <see cref="TriggerCondition.DuringYourTurn"/> (=true): it
/// narrows WHEN the leave-graveyard event must occur for the ability to
/// trigger, distinct from the type/count qualifiers on the event itself.
/// </para>
///
/// <para>
/// Anchored to the full trigger body (after the leading timing word is
/// stripped) so this cannot match as a substring inside a longer/compound
/// trigger clause handled by a more-specific sibling rule, and so the plain
/// untyped Quintorius shape (no type restriction, no turn qualifier) cannot
/// be mismatched by this rule (and vice versa).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 982)]
public sealed class ArtifactOrCreatureCardsLeaveGraveyardDuringYourTurnConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^one\s+or\s+more\s+artifact\s+and/or\s+creature\s+cards?\s+leave\s+your\s+graveyard\s+during\s+your\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (
      !lower.Contains("leave")
      || !lower.Contains("graveyard")
      || !lower.Contains("one or more")
      || !lower.Contains("during your turn")
    )
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
        CardTypes = ["artifact", "creature"],
        Controller = ControllerFilter.You,
      },
      MinimumCount = 1,
      DuringYourTurn = true,
    };
  }
}
