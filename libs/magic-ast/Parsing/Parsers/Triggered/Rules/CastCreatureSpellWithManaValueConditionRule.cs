namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "you cast a creature spell with mana value N or greater" — a cast trigger
/// narrowed by a mana-value threshold on the cast spell (Kalemne, Disciple of
/// Iroas: "Whenever you cast a creature spell with mana value 5 or greater,
/// you get an experience counter.").
///
/// <para>
/// Distinct from the general <see cref="SpellCastConditionRule"/> (priority 998)
/// which recognises card-type/color qualifiers on the cast spell but has no
/// mana-value axis, so it would silently drop the "with mana value N or
/// greater" threshold. Mirrors <see cref="CreatureWithManaValueEntersConditionRule"/>
/// (the enters-trigger sibling) by landing the threshold on the filter's
/// <see cref="ObjectFilter.ManaValueComparison"/> axis. CR 202.3 (mana value) /
/// CR 601 (casting spells) / CR 603.2 (a game event matching the trigger event
/// triggers the ability).
/// </para>
///
/// <para>
/// Runs at priority 999, above <see cref="SpellCastConditionRule"/> (998), so this
/// more-specific mana-value-gated form is matched first. End-anchored (mirrors
/// <see cref="ColorlessSpellCastConditionRule"/>/<see cref="CreatureWithManaValueEntersConditionRule"/>):
/// <paramref name="triggerText"/> still carries the leading timing word
/// ("Whenever"), so the pattern is NOT start-anchored, only word-boundary +
/// end-anchored, so it cannot substring-match a longer or differently-qualified
/// sibling clause.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class CastCreatureSpellWithManaValueConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"\byou\s+casts?\s+a\s+creature\s+spell\s+with\s+mana\s+value\s+(?<mv>\d+)\s+or\s+(?:greater|more)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("cast") || !lower.Contains("creature") || !lower.Contains("mana value"))
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
      Event = TriggerEvent.SpellCast,
      Filter = new ObjectFilter
      {
        CardTypes = ["spell", "creature"],
        Controller = ControllerFilter.You,
        ManaValueComparison = new Comparison
        {
          Operator = ComparisonOperator.GreaterThanOrEqual,
          Value = threshold,
        },
      },
    };
  }
}
