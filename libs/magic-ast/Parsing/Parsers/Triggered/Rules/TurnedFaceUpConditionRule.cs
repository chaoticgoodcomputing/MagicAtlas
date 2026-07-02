namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When this creature is turned face up" — Rule 702.37 (Morph/Megamorph) face-up trigger.
/// Fires when the face-down permanent's morph or megamorph cost is paid and the card
/// becomes face up (Rule 702.37e/f). Subject is always "this creature".
/// </summary>
[TriggerConditionRule(Priority = 982)]
public sealed class TurnedFaceUpConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("turned face up"))
    {
      return null;
    }

    // Subject filter: "this creature" is the only oracle shape for this trigger.
    var filter = new ObjectFilter { CardTypes = ["creature"] };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.TurnedFaceUp,
      Filter = filter,
    };
  }
}
