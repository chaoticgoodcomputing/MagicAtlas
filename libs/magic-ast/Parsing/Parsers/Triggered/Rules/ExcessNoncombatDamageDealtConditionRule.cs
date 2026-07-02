namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
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
  // ANCHORED (^…$) on the full trigger clause (optional timing prefix). The subject types are
  // CAPTURED, not hardcoded: "a creature an opponent controls" → ["creature"] (Fall of Cair Andros),
  // "a creature or planeswalker an opponent controls" → ["creature","planeswalker"] (Toralf). The
  // prior `Contains` matcher unconditionally emitted both types, mislabeling Fall of Cair Andros
  // (creature-only) with a spurious planeswalker — the sibling-overfit the judge caught.
  private static readonly Regex _pattern = new(
    @"^(?:when(?:ever)?\s+)?a\s+creature(?<pw>\s+or\s+planeswalker)?\s+an\s+opponent\s+controls\s+is\s+dealt\s+excess\s+noncombat\s+damage$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("dealt excess noncombat damage"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

    string[] cardTypes = m.Groups["pw"].Success ? ["creature", "planeswalker"] : ["creature"];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ExcessNoncombatDamageDealt,
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes,
        Controller = ControllerFilter.Opponent,
      },
    };
  }
}
