namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "enters or attacks" combined triggers — fires on either zone-change/combat event.
/// Oracle form: "Whenever this creature enters or attacks, [effect]."
/// Rule 603: "enters" (ETB, CR 400.4) and "attacks" (Declare Attackers step, CR 508)
/// are distinct game events; a single triggered ability may name both via the
/// "or" conjunction, triggering on whichever event occurs.
/// Must be tried before the individual Enters/Attacks rules (higher priority = 993)
/// so the disjunction isn't split and misclassified.
/// </summary>
[TriggerConditionRule(Priority = 993)]
public sealed class EntersOrAttacksConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters or attacks"))
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
      Event = TriggerEvent.EntersOrAttacks,
      Filter = filter,
    };
  }
}
