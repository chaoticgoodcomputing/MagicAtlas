namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Triggered-ability effect shape
/// "target [filter] [controller-clause] can't block this turn." — the
/// controller-scoped sibling of <see cref="CantBlockThisTurnTriggeredRule"/>,
/// which only recognizes an unqualified filter (e.g. "target creature can't
/// block this turn"). Here the target's controller axis is pinned by a trailing
/// possessive clause, e.g. Smelt-Ward Minotaur: "Whenever you cast an instant or
/// sorcery spell, target creature an opponent controls can't block this turn."
///
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." The trigger ("Whenever
/// you cast an instant or sorcery spell") is parsed separately; this rule
/// recognizes only the post-comma effect body.
///
/// CR 509.1 (declare-blockers step): "First, the defending player declares
/// blockers. This turn-based action doesn't use the stack. … If at any point
/// during the declaration of blockers, the defending player is unable to comply
/// with any of the steps listed below, the declaration is illegal…" A "can't
/// block" effect is a blocker-side restriction applied during this step; the
/// "this turn" qualifier makes it durative (expires at end of turn).
///
/// Maps to <see cref="CantBlockEffect"/> with a <c>Target</c> whose
/// <see cref="ObjectFilter.Controller"/> records the controller clause, and
/// <c>Duration</c> = end of turn. Anchored (^…$) so it never claims the
/// unqualified surface owned by <see cref="CantBlockThisTurnTriggeredRule"/>.
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class TargetControllerCantBlockThisTurnTriggeredRule : ITriggeredRule
{
  // "target <filter> <controller-clause> can't block this turn"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+(?<controller>an opponent controls|you control|defending player controls)\s+can'?t\s+block\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var baseFilter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (baseFilter is null)
    {
      return false;
    }

    var controller = m.Groups["controller"].Value.Trim().ToLowerInvariant() switch
    {
      "an opponent controls" => ControllerFilter.Opponent,
      "you control" => ControllerFilter.You,
      "defending player controls" => ControllerFilter.DefendingPlayer,
      _ => (ControllerFilter?)null,
    };
    if (controller is null)
    {
      return false;
    }

    effect = new CantBlockEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = baseFilter with { Controller = controller },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
