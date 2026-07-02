namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Activated-ability "anti-lure" effect shape "target [filter] can't block
/// this creature this turn." — e.g. Spin Engine: "{R}: Target creature can't
/// block this creature this turn."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are
/// written as \"[Cost]: [Effect.] [Activation instructions (if any).]\"
/// Example: The activation cost of an ability that reads \"{2}, {T}: You
/// gain 1 life\" is two mana of any type plus tapping the permanent that has
/// the ability." The cost container ({R}) is parsed independently; this rule
/// only recognizes the post-colon effect body.
///
/// CR 509.1: "First, the defending player declares blockers. This turn-based
/// action doesn't use the stack. To declare blockers, the defending player
/// follows the steps below, in order. If at any point during the
/// declaration of blockers, the defending player is unable to comply with
/// any of the steps listed below, the declaration is illegal; the game
/// returns to the moment before the declaration (see rule 733, \"Handling
/// Illegal Actions\"). A restriction may be created by an evasion ability (a
/// static ability an attacking creature has that restricts what can block
/// it). If an attacking creature gains or loses an evasion ability after a
/// legal block has been declared, it doesn't affect that block. Different
/// evasion abilities are cumulative..." This is a blocker-side restriction
/// placed on the targeted creature, naming the activating creature
/// (<c>Self</c>) as the specific attacker it can't block — the negative dual
/// of the "lure" requirement shape.
///
/// Maps to <see cref="CantBlockEffect"/> with <c>Target</c> = the targeted
/// restricted creature (parsed via
/// <see cref="SpellRuleHelpers.ParseTargetFilter"/>), <c>Blocks</c> =
/// <see cref="ObjectReference.Self"/> (this creature), and
/// <c>Duration</c> = end of turn, matching the oracle phrase "this turn".
/// </summary>
[ActivatedEffectRule(Priority = 81)]
public sealed class TargetCreatureCantBlockThisCreatureThisTurnEffectRule : IActivatedEffectRule
{
  // "target <filter> can't block this creature this turn"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+can'?t\s+block\s+this\s+creature\s+this\s+turn$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      Blocks = ObjectReference.Self(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
