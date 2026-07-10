namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "They gain [keyword] until end of turn." — grants a keyword ability, for the
/// remainder of the turn, to a plural group of objects named earlier in the same
/// ability (typically the tokens just created by a preceding sentence). Covers
/// Ovika, Enigma Goliath's second sentence: "create X 1/1 red Phyrexian Goblin
/// creature tokens, where X is the mana value of that spell. They gain haste
/// until end of turn."
///
/// <para>
/// Plural sibling of <see cref="ItGainsKeywordUntilEndOfTurnRule"/> (singular
/// "it gains …"). "They" refers anaphorically (CR 113.8b) to the group of objects
/// the previous sentence introduced; MAST records this the same way the singular
/// case does — <see cref="ObjectReferenceKind.It"/> is the generic anaphoric
/// pronoun regardless of grammatical number (mirrors the "them" → <c>It</c>
/// mapping already established for Valley Floodcaller's "Untap them.", and the
/// "they" → <c>It</c> mapping on <c>PoisonCounterDifferenceEffectRule</c>).
/// </para>
///
/// <para>
/// Rule 603: triggered abilities resolve by executing their effects in order.
/// Rule 611.1: continuous effects that grant keyword abilities have a duration;
/// "until end of turn" is the most common one for triggered keyword grants.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class TheyGainKeywordUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^they\s+gain\s+(?<keyword>[A-Za-z][a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var rawKeyword = m.Groups["keyword"].Value;
    var ability = TriggeredRuleHelpers.BuildKeywordStaticAbility(rawKeyword);
    if (ability is null)
    {
      // Unrecognised keyword — bail so fallback handles it; no free text.
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = ObjectReference.It(),
      GainedAbility = ability,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
