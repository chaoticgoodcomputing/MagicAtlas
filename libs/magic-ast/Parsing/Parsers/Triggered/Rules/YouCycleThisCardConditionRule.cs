namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When you cycle this card" — the cycling trigger condition (CR 702.29c).
///
/// <para>
/// CR 702.29c (verbatim): "'When you cycle this card' means 'When you discard this
/// card to pay an activation cost of a cycling ability.' These abilities trigger
/// from whatever zone the card winds up in after it's cycled."
/// </para>
///
/// <para>
/// The trigger fires on <see cref="TriggerEvent.Cycled"/> with
/// <see cref="ObjectFilter.IsSelf"/> = true, identifying that it is THIS card that
/// is being cycled. Anchored check prevents matching broader discard or draw
/// triggers.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 975)]
public sealed class YouCycleThisCardConditionRule : ITriggerConditionRule
{
  // Anchored on both ends so "when you cycle this card" doesn't match as a
  // substring of a different, more-specific condition.
  private static readonly Regex Pattern = new(
    @"^when\s+you\s+cycle\s+this\s+card\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("cycle"))
    {
      return null;
    }

    if (!Pattern.IsMatch(lower))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Cycled,
      Filter = new ObjectFilter { IsSelf = true },
    };
  }
}
