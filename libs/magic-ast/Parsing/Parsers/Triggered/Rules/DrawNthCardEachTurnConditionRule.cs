namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you draw your second card each turn" — ordinal-qualified draw trigger
/// (Rule 121: Drawing a Card). Unlike the unqualified
/// <see cref="DrawCardConditionRule"/> ("you draw a card"), this shape names a
/// specific occurrence within the turn ("your <i>second</i> card") and a per-turn
/// counting window ("each turn"). MAST records those two qualifiers descriptively
/// on the <see cref="TriggerCondition"/> (<see cref="TriggerCondition.Ordinal"/> and
/// <see cref="TriggerCondition.PerTurn"/>) rather than encoding any turn-state
/// counting machinery — Rule 603.2 makes the event-match the trigger; the ordinal
/// merely narrows which match counts. Controller defaults to You.
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class DrawNthCardEachTurnConditionRule : ITriggerConditionRule
{
  // Maps the ordinal words MTG oracle text uses for per-turn draw triggers to
  // their numeric value. "first" through "tenth" covers the printed range.
  private static readonly IReadOnlyDictionary<string, int> _ordinals = new Dictionary<
    string,
    int
  >(StringComparer.OrdinalIgnoreCase)
  {
    ["first"] = 1,
    ["second"] = 2,
    ["third"] = 3,
    ["fourth"] = 4,
    ["fifth"] = 5,
    ["sixth"] = 6,
    ["seventh"] = 7,
    ["eighth"] = 8,
    ["ninth"] = 9,
    ["tenth"] = 10,
  };

  // "you draw your <ordinal> card each turn" — the per-turn ordinal draw shape.
  // The "each turn" qualifier is required so the unqualified "you draw a card"
  // (DrawCardConditionRule) keeps owning its shape.
  private static readonly Regex _pattern = new(
    @"\byou\s+draw\s+your\s+(?<ordinal>\w+)\s+card\s+each\s+turn\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("draw") || !lower.Contains("card"))
    {
      return null;
    }

    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    if (!_ordinals.TryGetValue(match.Groups["ordinal"].Value, out var ordinal))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DrawsCard,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
      Ordinal = ordinal,
      PerTurn = true,
    };
  }
}
