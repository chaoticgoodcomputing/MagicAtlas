namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Activated-ability effect shape "target [filter] can't attack this turn." —
/// e.g. Netter en-Dal: "{W}, {T}, Discard a card: Target creature can't attack
/// this turn."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are written
/// as \"[Cost]: [Effect.] [Activation instructions (if any).]\"" The cost
/// container ({W}, {T}, Discard a card) is parsed independently; this rule
/// only recognizes the post-colon effect body.
///
/// CR 508.1c (declare-attackers step; attacking restrictions constrain the set
/// of legal attacker declarations the active player can make). A "can't
/// attack" effect is an attacker-side restriction under this rule.
///
/// Maps to <see cref="CantAttackEffect"/> with a <c>Target</c> reference
/// parsed via <see cref="SpellRuleHelpers.ParseTargetFilter"/> (giving the
/// same subtype/type generality as the blocker-side sibling
/// <c>CantBlockThisTurnEffectRule</c>) and <c>Duration</c> = end of turn,
/// matching the oracle phrase "this turn".
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class CantAttackThisTurnEffectRule : IActivatedEffectRule
{
  // "target <filter> can't attack this turn"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+can'?t\s+attack\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var filterPhrase = m.Groups["filter"].Value.Trim();
    var filter = SpellRuleHelpers.ParseTargetFilter(filterPhrase);
    if (filter is null)
    {
      return null;
    }

    return new CantAttackEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = filter,
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
