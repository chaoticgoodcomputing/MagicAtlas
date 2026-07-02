namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you roll one or more dice" (and the equivalent "roll a die" / "roll dice" forms) — the
/// die-roll trigger, CR 706. This is the dice CONSUMER side: the event a <c>RollDieEffect</c> (the
/// emitter) satisfies. Modeling it is what lets the interaction engine close a dice loop — roll → this
/// trigger → effect → … → roll again — so a self-feeding roll engine (e.g. Brazen Dwarf) is reconstructable.
///
/// <para>
/// A player-action trigger (no battlefield object), so no <see cref="TriggerCondition.Filter"/> — the
/// controller restriction ("you") is implicit. The trigger may carry a <i>result qualifier</i> on the
/// rolled value (CR 706.7 references comparing "the results of that roll … to a given number"):
/// <list type="bullet">
///   <item>"roll a 4 or higher" → a <i>minimum</i>, captured on
///   <see cref="TriggerCondition.DieResultThreshold"/> (Mr. House, Chittering Doom — the "on a die"
///   suffix is tolerated).</item>
///   <item>"roll a 1" / "roll a 1 or 2" → an <i>exact value set</i>, captured on
///   <see cref="TriggerCondition.DieResultValues"/> (Complaints Clerk, Atomwheel Acrobats).</item>
///   <item>no qualifier ("roll one or more dice") → fires on any roll.</item>
/// </list>
/// The two result qualifiers are mutually exclusive; a roll trigger carries at most one. CR 706.2:
/// "the final number is the result of the die roll."
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

  // "you roll a 4 or higher" / "you roll a 6 or higher" [optionally "... on a die"] — a result-threshold
  // (minimum) roll trigger (Mr. House; Chittering Doom adds the "on a die" suffix, tolerated by the
  // non-anchored tail). Checked before the exact-value form so "4 or higher" doesn't read as the bare
  // value "4".
  private static readonly Regex _threshold = new(
    @"\byou roll (?:a |an )?(?<n>\d+) or higher\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "you roll a 1" / "you roll a 1 or 2" / "you roll a 3, 4, or 5" [optionally "... on a die"] — an
  // exact-value roll trigger. The first value follows "a"/"an"; any additional values are an "or"/comma
  // series. NOT "or higher" (the threshold form is matched first and would otherwise capture the leading
  // digit here). Anchored on "you roll" so it can't read a stray digit elsewhere in the trigger phrase.
  private static readonly Regex _exactValue = new(
    @"\byou roll (?:a |an )?(?<first>\d+)(?<rest>(?:\s*,?\s*or\s+\d+|\s*,\s*\d+)*)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Pulls each additional value out of the "rest" tail (", or 2", " or 2", ", 4").
  private static readonly Regex _restValue = new(
    @"\d+",
    RegexOptions.Compiled
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

    // Exact rolled value(s): "you roll a 1" / "you roll a 1 or 2". Reuses the same DiceRolled event as
    // the plain and threshold forms; the named value(s) ride on DieResultValues (CR 706.7).
    var ev = _exactValue.Match(lower);
    if (ev.Success && int.TryParse(ev.Groups["first"].Value, out var first))
    {
      var values = new List<int> { first };
      foreach (Match m in _restValue.Matches(ev.Groups["rest"].Value))
      {
        if (int.TryParse(m.Value, out var v))
        {
          values.Add(v);
        }
      }

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.DiceRolled,
        DieResultValues = values,
      };
    }

    if (_plain.IsMatch(lower))
    {
      return new TriggerCondition { Timing = timing, Event = TriggerEvent.DiceRolled };
    }

    return null;
  }
}
