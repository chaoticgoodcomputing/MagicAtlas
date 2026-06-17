namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Trigger condition for "tap a [nonland] permanent for [mana/{C}]" shapes.
///
/// Handles two variants:
/// <list type="bullet">
///   <item>
///     Specific-symbol: "you tap a permanent for {C}" (Forsaken Monument) — the produced mana
///     symbol is recorded in <see cref="TriggerCondition.ProducedMana"/>. Controller: You when
///     "you tap"; Any when "a player taps".
///   </item>
///   <item>
///     Any-mana: "you tap a nonland permanent for mana" (Kinnan, Bonder Prodigy) — the trigger
///     fires on any mana production by tapping a nonland permanent. No specific symbol is
///     recorded (<see cref="TriggerCondition.ProducedMana"/> is null). The nonland qualifier is
///     expressed as <c>ExcludedCardTypes: ["land"]</c> on the filter.
///   </item>
/// </list>
///
/// Rule 605.1a: An activated ability is a mana ability if it could add mana to a player's mana
/// pool when it resolves and it doesn't require a target. Tapping a permanent for mana is such
/// an ability; this rule encodes the trigger that fires when that activation occurs.
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class TapForManaConditionRule : ITriggerConditionRule
{
  // "you tap a permanent for {C}" — the specific mana symbol in {braces}.
  // Anchors at the end (after the mana symbol) to ensure we match exactly this
  // shape and not a broader clause. triggerText includes the timing word
  // ("Whenever you tap a permanent for {C}"), so we use a non-start-anchored
  // pattern that captures the subject and mana symbol.
  private static readonly Regex _tapForManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+permanent\s+for\s+(?<mana>\{[A-Z0-9/]+\})\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "you tap a nonland permanent for mana" — Kinnan, Bonder Prodigy shape.
  // No specific mana symbol; the nonland qualifier excludes lands. ProducedMana is null.
  private static readonly Regex _tapNonlandForAnyManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+nonland\s+permanent\s+for\s+mana\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("tap") || !lower.Contains("permanent"))
    {
      return null;
    }

    // Try specific-symbol variant first (Forsaken Monument shape).
    var m = _tapForManaPattern.Match(triggerText.Trim());
    if (m.Success)
    {
      var subject = m.Groups["subject"].Value.Trim();
      var mana = m.Groups["mana"].Value.ToUpperInvariant();

      // "you tap" → Controller: You; "a player taps" → Controller: Any
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          Controller = controller,
        },
        ProducedMana = mana,
      };
    }

    // Try any-mana nonland variant (Kinnan, Bonder Prodigy shape).
    var n = _tapNonlandForAnyManaPattern.Match(triggerText.Trim());
    if (n.Success)
    {
      var subject = n.Groups["subject"].Value.Trim();
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          ExcludedCardTypes = ["land"],
          Controller = controller,
        },
        // ProducedMana is null — the trigger fires for any mana type.
      };
    }

    return null;
  }
}
