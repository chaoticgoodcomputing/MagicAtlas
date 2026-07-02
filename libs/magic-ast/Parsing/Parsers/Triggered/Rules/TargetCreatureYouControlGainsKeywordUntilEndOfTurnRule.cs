namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "target creature you control gains [keyword] until end of turn." — grants a
/// keyword ability to a chosen target creature under the trigger controller's
/// control, for the remainder of the turn. Covers Dinotomaton's ETB trigger
/// effect clause: "target creature you control gains menace until end of turn."
///
/// CR 115.1: "target creature" requires a chosen target; the "you control"
/// qualifier restricts the legal targets to creatures controlled by the
/// ability's controller, modelled on <see cref="ObjectReferenceKind.Target"/>
/// via <see cref="ObjectFilter.Controller"/> = <see cref="ControllerFilter.You"/>.
/// CR 611.1: the keyword grant is a continuous effect with a duration — "until
/// end of turn" is the fixed duration.
///
/// Distinct from <see cref="TargetCreatureGainsKeywordUntilEndOfTurnRule"/>,
/// which matches the bare "target creature gains ..." (no controller
/// qualifier) and would otherwise falsely match this text's "creature" as its
/// whole subject were "you control" not anchored between "creature" and
/// "gains" in this rule's own pattern.
///
/// CR 702.111 "Menace"; 702.111a "Menace is an evasion ability."; 702.111b "A
/// creature with menace can't be blocked except by two or more creatures.
/// (See rule 509, "Declare Blockers Step.")"; 702.111c "Multiple instances of
/// menace on the same creature are redundant."
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class TargetCreatureYouControlGainsKeywordUntilEndOfTurnRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+creature\s+you\s+control\s+gains?\s+(?<keyword>[A-Za-z][a-z]+(?:\s+[a-z]+)?)\s+until\s+end\s+of\s+turn\.?$",
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
    var ability = BuildKeywordAbility(rawKeyword.ToLowerInvariant().Trim())
      ?? TriggeredRuleHelpers.BuildKeywordStaticAbility(rawKeyword);
    if (ability is null)
    {
      // Unrecognised keyword — bail so fallback handles it; no free text.
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = ObjectReference.Target(new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
      }),
      GainedAbility = ability,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }

  // Menace is handled inline because TriggeredRuleHelpers.BuildKeywordStaticAbility's
  // switch has no "menace" case (it stops at deathtouch); reusing that helper alone
  // would bail on menace and leave the line unparsed. Mirrors
  // TeamGainKeywordTriggeredRule.BuildKeywordAbility's menace case verbatim.
  private static StaticAbility? BuildKeywordAbility(string keyword)
  {
    return keyword switch
    {
      "menace" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Menace,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
            MinimumBlockers = 2,
          },
        ],
      },
      _ => null,
    };
  }
}
