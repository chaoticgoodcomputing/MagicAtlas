namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever [subject] attacks for the first time each turn" — a once-per-turn
/// attack trigger that fires only on the subject's first attack in a given turn.
///
/// <para>Produces a <see cref="TriggerCondition"/> with
/// <see cref="TriggerEvent.Attacks"/>, <see cref="TriggerCondition.Ordinal"/> = 1,
/// and <see cref="TriggerCondition.PerTurn"/> = true, matching the encoding used for
/// other first-N-per-turn ordinal triggers (e.g. Lat-Nam Adept's second draw each
/// turn). CR 603.2: the trigger fires on the first event that matches the condition
/// within the qualifying window; subsequent attacks that turn do not re-trigger.</para>
///
/// <para>Priority 988 — above the generic <see cref="AttacksConditionRule"/> (Priority
/// 987) so this more-specific ordinal form is matched first. Both rules share the
/// same surface verb "attacks"; the ordinal qualifier "for the first time each turn"
/// distinguishes them. The pattern is anchored (^...$) so it cannot silently consume
/// a sibling trigger that merely contains "attacks for the first time" as a substring
/// of a longer phrase.</para>
///
/// <para>CR 603.2: triggered abilities trigger every time the event they specify
/// occurs; the Ordinal + PerTurn qualification narrows which occurrence of that event
/// actually fires the ability, recorded descriptively — the per-turn counter is engine
/// territory.</para>
/// </summary>
[TriggerConditionRule(Priority = 988)]
public sealed class AttacksFirstTimeEachTurnConditionRule : ITriggerConditionRule
{
  // "attacks for the first time each turn" — anchored on the right to prevent
  // this pattern from matching inside a longer phrase.
  private static readonly Regex _ordinalSuffix = new(
    @"\battacks\s+for\s+the\s+first\s+time\s+each\s+turn\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_ordinalSuffix.IsMatch(lower))
    {
      return null;
    }

    // Strip the ordinal suffix to get the bare subject text, then reuse the
    // standard object-filter helper to resolve the subject (self-by-name, "this
    // creature", etc.).
    var stripped = _ordinalSuffix.Replace(triggerText, string.Empty, count: 1).Trim();
    // Reconstruct a minimal attacks-style trigger text for ParseObjectFilter:
    // e.g. "Whenever Godo" → "Whenever Godo attacks" so IsSelfByNameTrigger fires.
    var forFilter = stripped + " attacks";

    var filter = TriggeredRuleHelpers.ParseObjectFilter(forFilter);
    if (filter is null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = filter,
      Ordinal = 1,
      PerTurn = true,
    };
  }
}
