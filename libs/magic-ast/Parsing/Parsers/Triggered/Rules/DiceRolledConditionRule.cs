namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you roll one or more dice" (and the equivalent "roll a die" / "roll dice" forms) — the
/// die-roll trigger, CR 706.2. This is the dice CONSUMER side: the event a <c>RollDieEffect</c> (the
/// emitter) satisfies. Modeling it is what lets the interaction engine close a dice loop — roll → this
/// trigger → effect → … → roll again — so a self-feeding roll engine (e.g. Brazen Dwarf) is reconstructable.
///
/// <para>
/// A player-action trigger (no battlefield object), so no <see cref="TriggerCondition.Filter"/> — the
/// controller restriction ("you") is implicit. An optional result threshold ("roll a 4 or higher" —
/// Mr. House) is captured on <see cref="TriggerCondition.DieResultThreshold"/>; without it the trigger
/// fires on any roll. CR 706.2: "If an effect instructs a player to roll two or more dice … abilities
/// that trigger whenever a player rolls one or more dice trigger only once."
/// </para>
///
/// <para>Priority 985 — a specific player-action phrase; anchored on the "roll" verb so it cannot be
/// confused with the emitter (an effect-side rule).</para>
/// </summary>
[TriggerConditionRule(Priority = 985)]
public sealed class DiceRolledConditionRule : ITriggerConditionRule
{
  // "you roll one or more dice" | "you roll a die" | "you roll dice"
  private static readonly Regex _plain = new(
    @"\byou roll (?:one or more dice|a die|dice)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "you roll a 4 or higher" / "you roll a 6 or higher" — a result-threshold roll trigger (Mr. House).
  private static readonly Regex _threshold = new(
    @"\byou roll (?:a |an )?(?<n>\d+) or higher\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var th = _threshold.Match(lower);
    if (th.Success && int.TryParse(th.Groups["n"].Value, out var n))
    {
      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.DiceRolled,
        DieResultThreshold = n,
      };
    }

    if (_plain.IsMatch(lower))
    {
      return new TriggerCondition { Timing = timing, Event = TriggerEvent.DiceRolled };
    }

    return null;
  }
}
