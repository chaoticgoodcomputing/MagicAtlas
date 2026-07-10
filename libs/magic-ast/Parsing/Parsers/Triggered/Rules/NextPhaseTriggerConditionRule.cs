namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "At the beginning of your next upkeep" / "…your next end step" / "…your next
/// turn" etc. — the ONE-SHOT delayed-trigger clock point (CR 603.7): a spell or
/// ability that, on resolution, sets up a triggered ability that fires exactly once
/// at the named point of the NEXT occurrence of that step. Representative cards: the
/// Pact family (Intervention Pact, Pact of Negation, Slaughter Pact, Summoner's
/// Pact, Pact of the Titan — "At the beginning of your next upkeep, pay [cost]…").
///
/// <para>
/// This is the "next" sibling of <see cref="PhaseTriggerConditionRule"/> (the
/// recurring "at the beginning of your upkeep" form on permanents). It maps the same
/// phase/step word to the clock point and the same possessive cue to the
/// <see cref="GameTime.Whose"/> axis, but additionally records the temporal qualifier
/// on <see cref="GameTime.When"/> = <see cref="TimeRelation.Next"/> — the single token
/// ("next") that distinguishes a one-shot delayed trigger from a permanent's recurring
/// upkeep trigger (ADR 0002 — the "this/next" occurrence is a property of the GameTime).
/// </para>
///
/// <para>
/// SCOPED to the "next" case only: returns null when the trigger text does not contain
/// "next", so the recurring form still falls through to
/// <see cref="PhaseTriggerConditionRule"/> unchanged. Runs at a higher priority so it
/// wins the "next" case (which <see cref="PhaseTriggerConditionRule"/> would otherwise
/// match while silently dropping the "next" qualifier). Only fires for At-timing
/// triggers.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 1100)]
public sealed class NextPhaseTriggerConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (timing != TriggerTiming.At)
    {
      return null;
    }

    if (!lower.Contains("beginning of") || !lower.Contains("next"))
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
    else if (lower.Contains("turn"))
    {
      part = TurnPart.Turn;
    }
    else if (lower.Contains("combat"))
    {
      part = TurnPart.Combat;
    }

    if (part is null)
    {
      return null;
    }

    // Possessive cue lands on the clock point's Whose axis (ADR 0002), mirroring
    // PhaseTriggerConditionRule.
    ControllerFilter? whose = null;
    if (lower.Contains("enchanted player"))
    {
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
    else if (lower.Contains("each player"))
    {
      whose = ControllerFilter.Any;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = new GameTime
      {
        Part = part.Value,
        Edge = TimeBoundary.Beginning,
        When = TimeRelation.Next,
        Whose = whose,
      },
    };
  }
}
