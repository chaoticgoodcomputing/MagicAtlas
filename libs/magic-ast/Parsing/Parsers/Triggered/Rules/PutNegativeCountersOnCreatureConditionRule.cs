namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "you put one or more -1/-1 counters on a creature" — counter-placement trigger (Nest of Scarabs).
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." This trigger fires whenever the
/// controller places one or more -1/-1 counters on any creature. The filter carries the
/// target card type (creature) and the controller (You) so the interaction layer knows whose
/// action triggered. The counter type is encoded in a companion <c>CounterType</c> field on
/// the <see cref="TriggerCondition"/> Filter-as-written (the creature being the object that
/// receives the counters).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 840)]
public sealed class PutNegativeCountersOnCreatureConditionRule : ITriggerConditionRule
{
  // "you put one or more -1/-1 counters on a creature"
  // Note: triggerText includes the timing word ("Whenever …") so we use a non-anchored match.
  private static readonly Regex _pattern = new(
    @"\byou\s+put\s+one\s+or\s+more\s+-1/-1\s+counters\s+on\s+a\s+creature\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("-1/-1"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.CounterPlaced,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
      },
    };
  }
}
