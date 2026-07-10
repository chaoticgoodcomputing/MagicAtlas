namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [CardName] becomes tapped, ..." — the self-by-name counterpart to
/// <see cref="BecomesTappedConditionRule"/>. Oracle text refers to the card by
/// its own short name rather than "this creature" (CR 201.5 — a card naming
/// itself refers to that object, the same self-reference convention already
/// used for "this creature").
///
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/>'s built-in self-by-name
/// recogniser (<see cref="TriggeredRuleHelpers.IsSelfByNameTrigger"/>) only
/// knows the "enters"/"dies"/"attacks"/"blocks" event-verb set, so it never
/// reaches the "becomes tapped" clause and <see cref="BecomesTappedConditionRule"/>
/// declines. This rule extends self-by-name coverage to the "becomes tapped"
/// event with its own structural regex (mirroring
/// <see cref="TriggeredRuleHelpers.IsSelfByNameTrigger"/>'s name-word heuristic)
/// without editing the shared helper.
///
/// CR 603.2: "Some trigger events use the word 'becomes' (for example, 'becomes
/// attached' or 'becomes blocked'). These trigger only at the time the named
/// event happens... An ability that triggers when a permanent 'becomes tapped'
/// or 'becomes untapped' doesn't trigger if the permanent enters the
/// battlefield in that state."
///
/// Example: "Whenever Kilo becomes tapped, proliferate." (Kilo, Apogee Mind).
///
/// Sits just below the generic <see cref="BecomesTappedConditionRule"/>
/// (Priority 985) and <see cref="SubtypeBecomesTappedConditionRule"/> (Priority
/// 984) so those plain-filter shapes get first refusal; both decline on a bare
/// self-by-name subject (no "a"/"an ... you control", no card-type word), so
/// dispatch order does not actually collide, but the lower priority keeps this
/// rule in its natural place among the "becomes tapped" family.
/// </summary>
[TriggerConditionRule(Priority = 983)]
public sealed class SelfByNameBecomesTappedConditionRule : ITriggerConditionRule
{
  // Name-word heuristic mirrors TriggeredRuleHelpers.IsSelfByNameTrigger: the
  // first word is capitalised (card names begin with a capital letter),
  // subsequent words may be capitalised content words or lowercase function
  // words that legally appear in card names, each optionally trailed by a
  // comma to accommodate legendary epithets (e.g. "Kari Zev, Skyship Raider").
  private const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";

  private static readonly Regex _pattern = new(
    @"^[A-Z][A-Za-z'\-]*,?(?:\s+(?:[A-Z][A-Za-z'\-]*|"
      + FunctionWords
      + @"),?)*\s+becomes\s+tapped\b",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("becomes tapped"))
    {
      return null;
    }

    // Strip the leading trigger timing keyword ("When"/"Whenever"/"At") before
    // testing the self-by-name shape, mirroring IsSelfByNameTrigger.
    var stripped = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );
    if (!_pattern.IsMatch(stripped))
    {
      return null;
    }

    // CR 201.5: a card naming itself refers to THAT object — a self-reference,
    // exactly like "this creature" (§6). Mark IsSelf so the interaction layer
    // sees a self-event trigger rather than an arbitrary-object one. The
    // "creature" type here is a DEFAULT: this layer has no type line, so a
    // non-creature self-by-name is retyped to its actual type downstream by
    // SelfReferenceTypeCorrector in CardParser — mirrors the convention in
    // TriggeredRuleHelpers.ParseObjectFilter's own self-by-name branch.
    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesTapped,
      Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
    };
  }
}
