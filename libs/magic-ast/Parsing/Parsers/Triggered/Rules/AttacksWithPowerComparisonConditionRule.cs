namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [a creature you control] with power N or less/greater attacks" — an
/// attack trigger whose subject filter carries a printed power threshold (Cavalcade
/// of Calamity: "Whenever a creature you control with power 1 or less attacks, …").
/// Produces the same <see cref="TriggerEvent.Attacks"/> condition as the generic
/// <see cref="AttacksConditionRule"/> (Rule 508 — Declare Attackers), but augments
/// the subject <see cref="ObjectFilter"/> with a <see cref="Comparison"/> on the
/// <see cref="ObjectFilter.PowerComparison"/> axis so the "with power N or less"
/// qualifier is not silently dropped.
///
/// <para>Priority 991 — above the generic <see cref="AttacksConditionRule"/>
/// (Priority 987), whose <c>ParseObjectFilter</c> ignores the power qualifier and
/// would otherwise claim the trigger first with a power-less filter. The pattern is
/// anchored on the right ("… with power N or (less|greater) attacks$") so it cannot
/// consume a sibling trigger that merely contains "attacks" elsewhere; the base
/// filter (card type + controller) is resolved by the shared
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> after the power clause is
/// stripped, keeping this rule's own surface additive.</para>
///
/// <para>CR 508.1a (declare attackers). The threshold is recorded descriptively on
/// the filter — MAST states the printed constraint; the engine evaluates it against
/// the actual power of the attacking creature.</para>
/// </summary>
[TriggerConditionRule(Priority = 991)]
public sealed class AttacksWithPowerComparisonConditionRule : ITriggerConditionRule
{
  // "… with power N or (less|greater) attacks" anchored at end of the trigger text.
  private static readonly Regex _pattern = new(
    @"\bwith\s+power\s+(?<n>\d+)\s+or\s+(?<cmp>less|greater)\s+attacks\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var m = _pattern.Match(lower);
    if (!m.Success)
    {
      return null;
    }

    // Strip the "with power N or less" qualifier so the shared filter helper sees a
    // bare "[subject] attacks" trigger and resolves the card type + controller axes.
    var stripped = _pattern.Replace(triggerText, " attacks", count: 1).Trim();
    var baseFilter = TriggeredRuleHelpers.ParseObjectFilter(stripped);
    if (baseFilter is null)
    {
      return null;
    }

    var op =
      string.Equals(m.Groups["cmp"].Value, "less", System.StringComparison.OrdinalIgnoreCase)
        ? ComparisonOperator.LessThanOrEqual
        : ComparisonOperator.GreaterThanOrEqual;

    var filter = baseFilter with
    {
      PowerComparison = new Comparison { Operator = op, Value = int.Parse(m.Groups["n"].Value) },
    };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = filter,
    };
  }
}
