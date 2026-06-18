namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [CardName] attacks an opponent" — emits
/// <see cref="TriggerEvent.AttacksAnOpponent"/> (CR 508 — Declare Attackers Step;
/// CR 102.2 — opponent = player not on the controller's team).
///
/// <para>
/// Distinguished from the plain <see cref="AttacksConditionRule"/> (Event: Attacks)
/// by the presence of "an opponent" after "attacks". The "attacks an opponent"
/// phrasing constrains the attack target to a player (not a planeswalker or battle),
/// so the trigger fires only when the creature is declared as attacking a player who
/// is an opponent of its controller.
/// </para>
///
/// <para>
/// Priority 990 — above <see cref="AttacksConditionRule"/> (Priority 987) so the
/// opponent-specific form is matched first and never falls through to the generic
/// "attacks" rule.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class AttacksAnOpponentConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Must contain "attacks an opponent" as a phrase.
    // Anchor: require the phrase "attacks an opponent" with word boundaries on
    // both sides so this rule cannot fire on "attacks an opponent's creature" or
    // similar extensions (no sub-string confusion with the plain Attacks rule).
    if (!lower.Contains("attacks an opponent"))
    {
      return null;
    }

    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.AttacksAnOpponent,
      Filter = filter,
    };
  }
}
