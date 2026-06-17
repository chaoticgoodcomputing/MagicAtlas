namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "it gains [keyword] until end of turn." — grants a keyword ability to the
/// entering object (referred to as "it") for the remainder of the turn.
/// Covers Dragon Tempest's first ability resolution clause: "it gains haste
/// until end of turn."
///
/// Rule 603: triggered abilities resolve by executing their effects. Rule 611.1:
/// effects that modify characteristics (such as keyword abilities) are
/// continuous effects with a duration. "Until end of turn" is the most common
/// duration for triggered keyword grants.
///
/// The target "it" refers anaphorically (CR 113.8b) to the permanent named by
/// the trigger's filter — the entering creature. Modelled as
/// <see cref="ObjectReferenceKind.It"/> (the generic anaphoric pronoun).
///
/// Distinct from <see cref="TeamGainKeywordTriggeredRule"/>, which targets
/// "creatures you control" (an <see cref="ObjectReferenceKind.Each"/> reference),
/// not the entering creature itself.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ItGainsKeywordUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^it\s+gains\s+(?<keyword>[A-Za-z][a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn\.?$",
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
