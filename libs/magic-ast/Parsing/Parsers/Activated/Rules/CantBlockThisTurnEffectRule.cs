namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Activated-ability effect shape "target [filter] can't block this turn." —
/// e.g. Sower of Chaos: "{2}{R}: Target creature can't block this turn."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are written
/// as \"[Cost]: [Effect.] [Activation instructions (if any).]\"" The cost
/// container ({2}{R}) is parsed independently; this rule only recognizes the
/// post-colon effect body.
///
/// CR 509.1b: "The defending player checks each creature they control to see
/// whether it's affected by any restrictions (effects that say a creature
/// can't block, or that it can't block unless some condition is met). If any
/// restrictions are being disobeyed, the declaration of blockers is illegal."
/// A "can't block" effect is a blocker-side restriction under this rule.
///
/// Maps to <see cref="CantBlockEffect"/> with a <c>Target</c> reference parsed
/// via <see cref="SpellRuleHelpers.ParseTargetFilter"/> (giving the same
/// subtype/type generality as the triggered-ability sibling
/// <c>CantBlockThisTurnTriggeredRule</c>) and <c>Duration</c> = end of turn,
/// matching the oracle phrase "this turn".
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class CantBlockThisTurnEffectRule : IActivatedEffectRule
{
  // "target <filter> can't block this turn"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+can'?t\s+block\s+this\s+turn$",
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

    return new CantBlockEffect
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
