namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "another target creature you control gains [keyword] until end of turn."
/// — grants a keyword ability to a separately chosen target creature the
/// controller controls, excluding the source itself, for the remainder of
/// the turn. Covers Void Grafter's ETB trigger effect clause: "another
/// target creature you control gains hexproof until end of turn."
///
/// Rule 603.1: triggered abilities have a trigger condition and an effect;
/// this rule handles the effect clause. Rule 115.1: "target creature"
/// requires a chosen target, modelled as <see cref="ObjectReferenceKind.Target"/>.
/// Rule 611.1: the keyword grant is a continuous effect with a duration —
/// "until end of turn" is the fixed duration, ending per rule 514.2 cleanup.
/// Hexproof itself is a static ability (CR 702.11a) meaning "This permanent
/// can't be the target of spells or abilities your opponents control"
/// (CR 702.11b).
///
/// Distinct from <see cref="TargetCreatureGainsKeywordUntilEndOfTurnRule"/>
/// (a bare "target creature", no controller/self restriction),
/// <see cref="ItGainsKeywordUntilEndOfTurnRule"/> (anaphoric "it", the
/// object named by the trigger's own filter), and
/// <see cref="EnchantedCreatureGainsKeywordUntilEndOfTurnRule"/> (the Aura's
/// fixed attachment, not a separately chosen target). Here the subject is
/// "another target creature you control": a chosen target
/// (<see cref="ObjectReferenceKind.Target"/>), restricted to the
/// controller's own creatures (<c>Controller = You</c>, CR 109.4), and
/// excluding the source object itself ("another", <c>ExcludeSelf = true</c>,
/// CR 109.5).
///
/// Builds the gained keyword ability with a small local switch rather than
/// calling <see cref="TriggeredRuleHelpers.BuildKeywordStaticAbility"/>,
/// which has no "hexproof" arm.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class AnotherTargetCreatureYouControlGainsKeywordUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^another\s+target\s+creature\s+you\s+control\s+gains?\s+(?<keyword>[A-Za-z][a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn\.?$",
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
    var ability = BuildKeywordStaticAbility(rawKeyword);
    if (ability is null)
    {
      // Unrecognised keyword — bail so fallback handles it; no free text.
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = ObjectReference.Target(
        new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
        }
      ),
      GainedAbility = ability,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  /// <summary>
  /// Local keyword builder — mirrors the shape of
  /// <see cref="TriggeredRuleHelpers.BuildKeywordStaticAbility"/> but adds a
  /// "hexproof" arm (CR 702.11a/b), which that shared helper does not cover.
  /// Kept local so this file stays collision-free with sibling rules.
  /// </summary>
  private static StaticAbility? BuildKeywordStaticAbility(string keywordRaw)
  {
    var lower = keywordRaw.ToLowerInvariant().Trim();
    if (lower != "hexproof")
    {
      return null;
    }

    return new StaticAbility
    {
      KeywordSource = KeywordAbility.Hexproof,
      Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Hexproof }],
    };
  }
}
