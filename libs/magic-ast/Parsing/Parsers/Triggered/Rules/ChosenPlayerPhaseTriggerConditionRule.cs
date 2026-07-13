namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "At the beginning of the chosen player's upkeep" (The Rack) — the clock-point
/// possessive bound to the CR 614.12 as-enters player choice
/// (<see cref="MagicAST.AST.Effects.Keyword.ChoosePlayerEffect"/>) rather than to
/// "your"/"each opponent's"/"each player's" (CR 109.5 — "that player" downstream
/// references resolve to whichever player the clock point names). Maps the
/// phase/step word to the same <see cref="TurnPart"/> vocabulary as
/// <see cref="PhaseTriggerConditionRule"/>, binding <see cref="GameTime.Whose"/> to
/// <see cref="ControllerFilter.ChosenPlayer"/>.
///
/// <para>
/// A SEPARATE rule rather than an added branch on <see cref="PhaseTriggerConditionRule"/>
/// (a shared, high-traffic fallback with no per-branch anchor of its own): this rule
/// requires the ANCHORED "chosen player" cue and is dispatched at a higher priority
/// so it intercepts before the shared rule would otherwise match with an unset
/// (null) <c>Whose</c>, silently losing the chosen-player binding. "chosen player"
/// never appears in the "your"/"each opponent"/"each player"/"enchanted player" cues
/// the shared rule already checks, so the two rules are disjoint and this addition
/// cannot regress any sibling.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 1010)]
public sealed class ChosenPlayerPhaseTriggerConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (timing != TriggerTiming.At)
    {
      return null;
    }

    if (!lower.Contains("beginning of") || !lower.Contains("chosen player"))
    {
      return null;
    }

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

    return new TriggerCondition
    {
      Timing = timing,
      Event = new GameTime
      {
        Part = part.Value,
        Edge = TimeBoundary.Beginning,
        Whose = ControllerFilter.ChosenPlayer,
      },
    };
  }
}
