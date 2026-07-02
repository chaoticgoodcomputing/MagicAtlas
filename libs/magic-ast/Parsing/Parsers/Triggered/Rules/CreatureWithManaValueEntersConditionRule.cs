namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "a creature with mana value N or greater enters" — a creature ETB trigger
/// narrowed by a mana-value threshold on the entering creature (the Dragon-Aura
/// cycle: Dragon Fangs / Dragon Scales / Dragon Breath / Dragon Wings / Dragon
/// Shadow). The threshold lands on the filter's
/// <see cref="ObjectFilter.ManaValueComparison"/> axis as a literal
/// <see cref="Comparison"/>.
///
/// <para>
/// CR 603.2: a game event matching the trigger event triggers the ability. The
/// "with mana value N or greater" qualifier (CR 202.3 / CR 107.1) restricts the
/// matching set to entering creatures whose mana value is at least N. The subject
/// is a plain "a creature" (not "this creature", not "another creature") so the
/// trigger watches ANY creature entering — including ones an opponent controls —
/// with no self-reference and no controller restriction.
/// </para>
///
/// <para>
/// Runs at priority 992 — above the general <see cref="EntersConditionRule"/> (990),
/// which would otherwise swallow "a creature ... enters" via the bare "a creature"
/// branch of <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> and silently drop
/// the mana-value threshold. Below the more specific colour/colorless/another rules
/// (995–997) so those still win for their narrower shapes.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class CreatureWithManaValueEntersConditionRule : ITriggerConditionRule
{
  // "a creature with mana value <N> or {greater|more} enters[[ the battlefield]]".
  // End-anchored; the "the battlefield" suffix is optional (modern oracle omits it).
  // Not anchored at start: the timing word ("When"/"Whenever") is still present.
  private static readonly Regex _pattern = new(
    @"\ba\s+creature\s+with\s+mana\s+value\s+(?<mv>\d+)\s+or\s+(?:greater|more)\s+enters(?:\s+the\s+battlefield)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("creature") || !lower.Contains("mana value") || !lower.Contains("enters"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText.Trim());
    if (!m.Success || !int.TryParse(m.Groups["mv"].Value, out var threshold))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        ManaValueComparison = new Comparison
        {
          Operator = ComparisonOperator.GreaterThanOrEqual,
          Value = threshold,
        },
      },
    };
  }
}
