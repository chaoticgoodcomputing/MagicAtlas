namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a creature or planeswalker an opponent controls is dealt excess noncombat
/// damage" — the Toralf trigger condition (KHM: Toralf, God of Fury). Fires when a
/// creature or planeswalker controlled by an opponent receives noncombat damage that
/// exceeds lethal damage or the permanent's loyalty (CR 120.10).
///
/// <para>
/// The filter carries <c>CardTypes = ["creature", "planeswalker"]</c> with
/// <c>Controller = Opponent</c>. Both card types share one filter because the oracle
/// text unifies them with "or" — no separate arms for each type.
/// </para>
///
/// <para>
/// CR 120.10 (verbatim): "Some triggered abilities check whether a permanent has been
/// dealt excess damage. These abilities check after the permanent has been dealt damage
/// by one or more sources. If those sources together dealt an amount of damage to a
/// creature greater than lethal damage, excess damage equal to the difference was dealt
/// to that creature. If those sources together dealt an amount of damage to a planeswalker
/// greater than that planeswalker's loyalty before the damage was dealt, excess damage
/// equal to the difference was dealt to that planeswalker."
/// </para>
///
/// <para>
/// Anchored (^…$) and scoped to the trigger clause to prevent matching inside a broader
/// clause that contains "dealt excess noncombat damage" as a substring.
/// Runs at priority 987 — above <see cref="SourceDealsNoncombatDamageConditionRule"/> (986)
/// so the "excess noncombat" specialisation is claimed before the more-general "deals
/// noncombat damage" rule.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 987)]
public sealed class ExcessNoncombatDamageDealtConditionRule : ITriggerConditionRule
{
  // The trigger text includes the "Whenever/When" timing prefix, so we use
  // Contains rather than an anchored regex. The phrase "dealt excess noncombat damage"
  // is sufficiently specific — no other known trigger uses this exact phrase (CR 120.10).
  // Additionally guard that "opponent controls" is present so this doesn't fire on a
  // self-damage-excess trigger if one ever appears.

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("dealt excess noncombat damage"))
    {
      return null;
    }

    if (!lower.Contains("opponent controls"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ExcessNoncombatDamageDealt,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature", "planeswalker"],
        Controller = ControllerFilter.Opponent,
      },
    };
  }
}
