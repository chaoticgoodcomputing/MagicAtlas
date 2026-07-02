namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you activate an ability" — the bare activated-ability trigger condition
/// (Rings of Brighthearth). Emits <see cref="TriggerEvent.AbilityActivated"/> with a
/// <c>Controller = You</c> filter (the ability you activate).
///
/// <para>
/// CR 603.2 / CR 602.1: an activated ability is "[cost]: [effect]" put on the stack by
/// its controller; "whenever you activate an ability" is a triggered ability that fires
/// on that act. The intervening-if "if it isn't a mana ability" (CR 605) is handled
/// separately by <see cref="MagicAST.Parsing.ConditionParser"/> as a
/// <see cref="MagicAST.AST.Abilities.TriggeringAbilityIsManaCondition"/>; this rule owns
/// only the trigger event + subject filter.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>) deliberately: "you activate an ability" recurs as a SUBSTRING of
/// more-specific siblings ("…activate an ability of a creature", "…activate an ability that
/// targets only…", "…activate a loyalty ability"). An unanchored matcher would mislabel
/// those, dropping their qualifiers — the #1 overfit FAIL class. The bare form (the post-split
/// <c>triggerPart</c> is exactly "Whenever you activate an ability") matches; every qualified
/// sibling carries extra words after "ability" and falls through to its own rule.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 900)]
public sealed class ActivateAbilityConditionRule : ITriggerConditionRule
{
  // The optional timing prefix is tolerated but the phrase is otherwise exact: any
  // trailing qualifier ("…ability of…", "…ability that…") fails the end anchor.
  private static readonly Regex _pattern = new(
    @"^(?:when(?:ever)?\s+)?you\s+activate\s+an\s+ability$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("activate"))
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
      Event = TriggerEvent.AbilityActivated,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }
}
