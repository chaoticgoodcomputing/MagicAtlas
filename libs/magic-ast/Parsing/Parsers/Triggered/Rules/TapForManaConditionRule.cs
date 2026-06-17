namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "you tap a permanent for {C}" / "a player taps a permanent for {C}" — trigger condition
/// for the mana-doubling shape on Forsaken Monument (ZNR) and similar colorless-mana doublers.
///
/// Rule 605.1a: An activated ability is a mana ability if it could add mana to a player's mana
/// pool when it resolves and it doesn't require a target. Tapping a permanent that produces {C}
/// is such an ability; this rule encodes the trigger that fires when that activation occurs.
///
/// The produced mana symbol is recorded in <see cref="TriggerCondition.ProducedMana"/> ({C}),
/// and the tapped permanent filter (CardTypes: ["permanent"], Controller: You for "you tap")
/// is recorded in <see cref="TriggerCondition.Filter"/>.
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

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("tap") || !lower.Contains("permanent"))
    {
      return null;
    }

    var m = _tapForManaPattern.Match(triggerText.Trim());
    if (!m.Success)
    {
      return null;
    }

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
}
