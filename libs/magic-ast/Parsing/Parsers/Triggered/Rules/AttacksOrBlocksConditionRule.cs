namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] attacks or blocks" — combined combat trigger (Rule 508/509),
/// modelled as a single <see cref="TriggerEvent.AttacksOrBlocks"/> event. Tried before
/// the individual Attacks/Blocks rules (higher priority) so the disjunction isn't
/// partially matched.
/// </summary>
[TriggerConditionRule(Priority = 989)]
public sealed class AttacksOrBlocksConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("attacks or blocks"))
    {
      return null;
    }

    // Strip "or blocks" so ParseObjectFilter sees a clean "attacks" subject phrase.
    var stripped = Regex.Replace(
      triggerText,
      @"\s+or\s+blocks\b",
      string.Empty,
      RegexOptions.IgnoreCase
    );
    var filter = TriggeredRuleHelpers.ParseObjectFilter(stripped);
    if (filter == null)
    {
      // Also try self-by-name with the "attacks or blocks" verb form.
      if (!IsSelfByNameAttacksOrBlocksTrigger(triggerText))
      {
        return null;
      }
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.AttacksOrBlocks,
      Filter = filter,
    };
  }

  /// <summary>
  /// Detects the "[CardName] attacks or blocks" self-by-name shape, extending
  /// <see cref="TriggeredRuleHelpers.IsSelfByNameTrigger"/> to cover the combined
  /// verb "attacks or blocks".
  /// </summary>
  private static bool IsSelfByNameAttacksOrBlocksTrigger(string triggerText)
  {
    var stripped = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );
    const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";
    return Regex.IsMatch(
      stripped,
      @"^[A-Z][A-Za-z'\-]*(?:\s+(?:[A-Z][A-Za-z'\-]*|" + FunctionWords + @"))*\s+attacks\s+or\s+blocks\b",
      RegexOptions.CultureInvariant
    );
  }
}
