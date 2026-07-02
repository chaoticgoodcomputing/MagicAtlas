namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "At the beginning of your upkeep" / "...first main phase" / "...draw step" /
/// "...end step" / "...combat on your turn". Maps the phase/step word to a
/// <see cref="TriggerEvent"/>. The "your" / "each opponent's" possessive lands on
/// the filter as a <c>Controller</c>. Only fires for At-timing triggers.
/// </summary>
[TriggerConditionRule(Priority = 1000)]
public sealed class PhaseTriggerConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (timing != TriggerTiming.At)
    {
      return null;
    }

    if (!lower.Contains("beginning of"))
    {
      return null;
    }

    // Map the phase/step word to the clock point (ADR 0002 — GameTime, not an enum member).
    TurnPart? part = null;
    if (lower.Contains("upkeep"))
    {
      part = TurnPart.Upkeep;
    }
    else if (lower.Contains("first main phase") || lower.Contains("precombat main phase"))
    {
      part = TurnPart.PrecombatMain;
    }
    else if (lower.Contains("postcombat main phase") || lower.Contains("second main phase"))
    {
      part = TurnPart.PostcombatMain;
    }
    else if (lower.Contains("draw step"))
    {
      part = TurnPart.Draw;
    }
    else if (lower.Contains("end step"))
    {
      part = TurnPart.End;
    }
    else if (lower.Contains("combat"))
    {
      part = TurnPart.Combat;
    }

    if (part is null)
    {
      return null;
    }

    // Possessive cue lands on the clock point's Whose axis (ADR 0002 — "your upkeep"
    // is a property of the time, not a Filter.Controller). "your" → You,
    // "each opponent's" → Opponent, "each player's" → unqualified.
    ControllerFilter? whose = null;
    if (lower.Contains("enchanted player"))
    {
      // "enchanted player's upkeep" on a player-enchanting Aura — the clock
      // point belongs to the enchanted player (CR 702.5, player Aura), not the
      // ability's own controller. Parallels the EnchantedPlayer controller axis.
      whose = ControllerFilter.EnchantedPlayer;
    }
    else if (lower.Contains("your"))
    {
      whose = ControllerFilter.You;
    }
    else if (lower.Contains("each opponent"))
    {
      whose = ControllerFilter.Opponent;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = new GameTime { Part = part.Value, Edge = TimeBoundary.Beginning, Whose = whose },
    };
  }
}
