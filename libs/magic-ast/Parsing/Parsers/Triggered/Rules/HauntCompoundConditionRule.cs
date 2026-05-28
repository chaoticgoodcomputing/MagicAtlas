namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When this creature enters or the creature it haunts dies" — Haunt compound
/// trigger (Rule 702.55). Fires in two situations: (1) when this creature enters
/// the battlefield, and (2) when the creature this card haunts dies. Both events
/// are modelled as the single <see cref="TriggerEvent.EntersOrHauntedCreatureDies"/>.
/// Must be tried before the generic enters/dies rules (higher priority) — this
/// trigger contains both words.
/// </summary>
[TriggerConditionRule(Priority = 993)]
public sealed class HauntCompoundConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^when\s+this\s+creature\s+enters\s+or\s+the\s+creature\s+it\s+haunts\s+dies$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("enters") || !lower.Contains("haunts") || !lower.Contains("dies"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.EntersOrHauntedCreatureDies,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };
  }
}
