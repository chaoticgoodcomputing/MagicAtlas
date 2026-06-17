namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a -1/-1 counter is put on a creature" — passive counter-placement trigger (Flourishing Defenses).
///
/// <para>
/// CR 122.1: "A counter is a marker placed on an object or player that modifies its
/// characteristics and/or interacts with a rule or effect." This trigger fires whenever
/// any player places a -1/-1 counter on any creature (passive voice: "is put on").
/// Unlike <see cref="PutNegativeCountersOnCreatureConditionRule"/> which covers
/// "you put one or more -1/-1 counters on a creature" (controller-scoped, "one or more"
/// quantity), this rule covers the broader "a [counter] is put on a creature" (any player,
/// single counter, passive). The event is still <see cref="TriggerEvent.CounterPlaced"/>
/// with <c>CounterType="-1/-1"</c> and <c>MinimumCount=1</c> (any single counter placement
/// fires this trigger).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 839)]
public sealed class CounterPutOnCreaturePassiveConditionRule : ITriggerConditionRule
{
  // "a -1/-1 counter is put on a creature"
  // Matches passive-voice single-counter trigger (no "you put", no "one or more").
  private static readonly Regex _pattern = new(
    @"\ba\s+-1/-1\s+counter\s+is\s+put\s+on\s+a\s+creature\b",
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
      CounterType = "-1/-1",
      MinimumCount = 1,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
      },
    };
  }
}
