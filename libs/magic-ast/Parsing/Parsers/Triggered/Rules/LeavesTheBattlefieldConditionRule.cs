namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Triggers;

/// <summary>
/// "leaves the battlefield" triggers: "this creature leaves the battlefield",
/// "a creature you control leaves the battlefield", etc. (CR 603.2 — a game event
/// matching a triggered ability's trigger event causes it to trigger).
///
/// This is the LTB half of a CR 607.2 linked pair on cards like Petravark, whose
/// ETB exiles a permanent and whose LTB returns the card exiled by that same
/// ability. MAST records the trigger event descriptively; the linkage to the
/// exiled card is carried on the effect side via the <c>ExiledWith</c> reference,
/// not threaded here (ADR 0004 reference-not-resolution).
/// </summary>
[TriggerConditionRule(Priority = 989)]
public sealed class LeavesTheBattlefieldConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("leaves the battlefield"))
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
      Event = TriggerEvent.LeavesTheBattlefield,
      Filter = filter,
    };
  }
}
