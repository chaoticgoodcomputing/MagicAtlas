namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever an opponent draws their second card each turn" — ordinal-qualified
/// draw trigger (CR 603.2 event-match; CR 121.1 drawing a card — "A player draws
/// a card by putting the top card of their library into their hand", the same
/// citation used by the sibling <see cref="OpponentDrawsCardConditionRule"/>)
/// scoped to an opponent subject, sibling to <see cref="DrawNthCardEachTurnConditionRule"/>'s
/// "you draw your &lt;ordinal&gt; card each turn" shape and
/// <see cref="OpponentDrawsCardConditionRule"/>'s unqualified "an opponent draws
/// a card". Neither existing rule's pattern covers this shape: the possessive
/// pronoun flips from "your" to "their" and the object is qualified by both an
/// ordinal ("second") and a per-turn counting window ("each turn"). MAST records
/// those two qualifiers descriptively on the <see cref="TriggerCondition"/>
/// (<see cref="TriggerCondition.Ordinal"/> and <see cref="TriggerCondition.PerTurn"/>)
/// rather than encoding turn-state counting machinery — the ordinal merely narrows
/// which draw-event match fires the ability. Filter Controller = Opponent.
/// </summary>
[TriggerConditionRule(Priority = 996)]
public sealed class OpponentDrawsNthCardEachTurnConditionRule : ITriggerConditionRule
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

  // "an opponent draws their <ordinal> card each turn" — the per-turn ordinal
  // draw shape scoped to an opponent subject.
  private static readonly Regex _pattern = new(
    @"\ban\s+opponent\s+draws\s+their\s+(?<ordinal>\w+)\s+card\s+each\s+turn\b",
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
      Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
      Ordinal = ordinal,
      PerTurn = true,
    };
  }
}
