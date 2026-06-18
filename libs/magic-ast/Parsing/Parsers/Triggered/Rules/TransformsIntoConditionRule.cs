namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Triggers;

/// <summary>
/// "As this creature transforms into [Name]" — transform-into trigger condition.
/// CR 603.6: an ability that uses "as [permanent] transforms into [Name]" fires
/// when the permanent transforms; it is functionally a "When" trigger with timing
/// simultaneous to the transformation event.
///
/// <para>
/// Produces <see cref="TriggerEvent.Transforms"/> with Filter carrying the source
/// type and <c>IsSelf = true</c> (the object being transformed is the source itself,
/// CR 201.5 — "this [type]" in oracle text is a self-reference). Timing is always
/// <see cref="TriggerTiming.When"/> (the "As" keyword maps to When by CR 603.6;
/// the caller supplies the timing having already detected the "As" prefix via the
/// raw-text intercept in <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/>).
/// </para>
///
/// <para>ANCHORED (<c>^…</c>): the guard checks for the exact "As this (creature|permanent)
/// transforms into" prefix so the rule cannot misfire on a sibling trigger.</para>
///
/// <para>Priority 999: transform-into triggers are highly specific and must run before
/// any generic <see cref="TriggerEvent.Transforms"/> rule that might match on "transforms"
/// alone.</para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class TransformsIntoConditionRule : ITriggerConditionRule
{
  // ANCHORED prefix: "As this creature/permanent transforms into [Name]"
  private static readonly Regex _pattern = new(
    @"^As\s+this\s+(creature|permanent)\s+transforms\s+into\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(triggerText))
    {
      return null;
    }

    var filter = TriggeredRuleHelpers.ParseObjectFilter(triggerText);
    if (filter is null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Transforms,
      Filter = filter,
    };
  }
}
