namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "one or more +1/+1 counters are put on this creature" — passive counter-placement
/// trigger on the source creature itself (Scurry Oak, Spike Feeder variants).
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." This trigger fires whenever
/// one or more +1/+1 counters are placed on the ability's source creature (passive voice:
/// "are put on this creature"). The filter carries <c>IsSelf = true</c> to record that
/// the recipient is the source permanent, not an arbitrary creature, so the interaction
/// layer can distinguish "counters placed on this specific permanent" from "counters
/// placed on any creature". The counter type is encoded in <see cref="TriggerCondition.CounterType"/>
/// and the minimum quantity in <see cref="TriggerCondition.MinimumCount"/>.
/// </para>
///
/// <para>
/// Rule 603.2: each event matching the trigger condition fires the ability; "one or more"
/// constrains the per-event count to at least 1 (MinimumCount = 1). The event fires on
/// any +1/+1 counter placement — by the controller or any other source — as long as the
/// recipient is this creature.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 841)]
public sealed class PutPlusCountersOnThisCreatureConditionRule : ITriggerConditionRule
{
  // "one or more +1/+1 counters are put on this creature"
  // Note: triggerText includes the timing word ("Whenever …") so we use a non-anchored match.
  private static readonly Regex _pattern = new(
    @"\bone\s+or\s+more\s+\+1/\+1\s+counters\s+are\s+put\s+on\s+this\s+creature\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("+1/+1"))
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
      CounterType = "+1/+1",
      MinimumCount = 1,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        IsSelf = true,
      },
    };
  }
}
