namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "another nontoken creature you control enters" — nontoken-filtered creature ETB
/// trigger, excluding the source permanent (the "another" qualifier) and excluding
/// token creatures (the "nontoken" qualifier).
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger
/// event, that ability automatically triggers." The "another" qualifier (CR 109.5)
/// excludes the source permanent from the matching set; the "nontoken" qualifier
/// (CR 111.1 — a token is not a card) narrows which entering creatures match.
/// </para>
///
/// <para>
/// Uses <see cref="ObjectFilter.IsToken"/> = <c>false</c> to encode the nontoken
/// qualifier — a flat boolean axis distinct from the card-type axes. CR 111.1:
/// "A token is a marker used to represent any permanent that isn't represented by a
/// card." Nontoken creatures are thus cards that are also creatures.
/// </para>
///
/// <para>
/// Running at priority 997 — tried before <see cref="AnotherColorCreatureEntersConditionRule"/>
/// (996) and well before the general <see cref="EntersConditionRule"/> (990). Anchored
/// on the subject prefix: "another nontoken creature" must appear so a broader sibling
/// such as "a creature you control enters" does not claim this shape.
/// </para>
///
/// <para>
/// CR 603.2 (verbatim): "Whenever a game event or game state matches a triggered
/// ability's trigger event, that ability automatically triggers."
/// CR 111.1 (verbatim): "Some effects put tokens onto the battlefield. A token is a
/// marker used to represent any permanent that isn't represented by a card."
/// </para>
///
/// <para>
/// Example: Preston, the Vanisher (BLB) — "Whenever another nontoken creature you
/// control enters, if it wasn't cast, create a token that's a copy of that creature,
/// except it's a 0/1 white Illusion."
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class AnotherNontokenCreatureEntersConditionRule : ITriggerConditionRule
{
  // Matches "another nontoken creature you control enters[[ the battlefield]]"
  // End-anchored; the "the battlefield" suffix is optional (modern oracle omits it).
  // Not anchored at start: the timing word ("Whenever") is still present in triggerText.
  private static readonly Regex _pattern = new(
    @"\banother\s+nontoken\s+creature\s+you\s+control\s+enters(?:\s+the\s+battlefield)?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("nontoken") || !lower.Contains("creature") || !lower.Contains("enters"))
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
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        IsToken = false,
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      },
    };
  }
}
