namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "remove a +1/+1 counter from it at end of combat" — Phantom-creature counter-removal
/// pattern (Judgment set). Fires as the resolution effect of an attacks-or-blocks triggered
/// ability; "it" is the pronoun for the creature that declared as an attacker or blocker.
///
/// <para>
/// Rule 122.3: removing a counter from a permanent means taking one counter of the specified
/// type off that permanent. Rule 511: the end of combat step is the last step of the combat
/// phase; delayed removal effects scheduled "at end of combat" resolve here.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class RemoveCountersTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();

    if (!lower.Contains("remove") || !lower.Contains("counter"))
    {
      return false;
    }

    // Extract counter type. Handles "+1/+1" / "-1/-1" P/T counters and named counters.
    string counterType;
    if (text.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (text.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      var namedMatch = Regex.Match(
        text,
        @"\bremove\s+a(?:n)?\s+(?<type>[\w\-]+)\s+counter\b",
        RegexOptions.IgnoreCase
      );
      if (!namedMatch.Success)
      {
        return false;
      }
      counterType = namedMatch.Groups["type"].Value.ToLowerInvariant();
    }

    // Count: default 1; respect "two", "three", digit literals.
    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(text) ?? 1;

    // Target: "from it" → It (pronoun for the triggering creature), "from this creature" → Self.
    ObjectReference target;
    if (Regex.IsMatch(lower, @"\bfrom\s+it\b"))
    {
      target = ObjectReference.It();
    }
    else if (
      Regex.IsMatch(lower, @"\bfrom\s+this\s+creature\b")
      || Regex.IsMatch(lower, @"\bfrom\s+this\s+permanent\b")
    )
    {
      target = ObjectReference.Self();
    }
    else if (Regex.IsMatch(lower, @"\bfrom\s+target\s+creature\b"))
    {
      target = ObjectReference.Target(new ObjectFilter { CardTypes = ["creature"] });
    }
    else
    {
      // No recognised target shape — let the fallback parser record the gap.
      return false;
    }

    // "at end of combat" is a delayed trigger (CR 603.7), not a duration — the
    // removal fires at the end-of-combat step. "until end of turn" is a genuine
    // continuous-effect duration. (ADR 0002/0004.)
    if (Regex.IsMatch(lower, @"\bat\s+end\s+of\s+combat\b"))
    {
      effect = new MagicAST.AST.Effects.Core.CreateDelayedTriggerEffect
      {
        DelayedTrigger = new MagicAST.AST.Abilities.DelayedTriggeredAbility
        {
          Trigger = new MagicAST.AST.Triggers.TriggerCondition
          {
            Timing = MagicAST.AST.Triggers.TriggerTiming.At,
            Event = new MagicAST.AST.References.GameTime
            {
              Part = MagicAST.AST.References.TurnPart.Combat,
              Edge = MagicAST.AST.References.TimeBoundary.End,
            },
          },
          Effects = [new RemoveCountersEffect { Target = target, CounterType = counterType, Count = LiteralQuantity.Of(count) }],
        },
      };
      return true;
    }

    Duration? duration = lower.Contains("until end of turn") ? UntilTimeDuration.EndOfTurn : null;
    effect = new RemoveCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
      Duration = duration,
    };
    return true;
  }
}
