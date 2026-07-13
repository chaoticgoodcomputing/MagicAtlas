namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Activated-ability attacker-side block requirement "Target [filter] must be
/// blocked this turn if able." — e.g. Satyr Piper: "{3}{G}: Target creature must
/// be blocked this turn if able."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are written
/// as \"[Cost]: [Effect.] [Activation instructions (if any).]\" Example: The
/// activation cost of an ability that reads \"{2}, {T}: You gain 1 life\" is
/// two mana of any type plus tapping the permanent that has the ability." The
/// cost container ({3}{G}) is parsed independently; this rule only recognizes
/// the post-colon effect body.
///
/// CR 509.1c: "The defending player checks each creature they control to see
/// whether it's affected by any requirements (effects that say a creature must
/// block, or that it must block if some condition is met). ... If a
/// requirement that says a creature blocks if able during a certain turn
/// refers to a turn with multiple combat phases, the creature blocks if able
/// during each declare blockers step in that turn." This compels the
/// defending player's OTHER creatures to block the named (targeted) creature —
/// the dual of <see cref="TargetCreatureBlocksThisCreatureThisTurnEffectRule"/>,
/// which compels the named creature itself to block a specific attacker.
///
/// Maps to <see cref="MustBeBlockedEffect"/> with <c>Target</c> = the targeted
/// creature that must be blocked (parsed via
/// <see cref="SpellRuleHelpers.ParseTargetFilter"/>) and <c>Duration</c> = end
/// of turn, matching the oracle phrase "this turn". Same effect shape as the
/// spell-resolution <see cref="MagicAST.Parsing.Parsers.Spell.Rules.MustBeBlockedTargetRule"/>
/// (Irresistible Prey), wired into the activated-ability effect path instead.
/// </summary>
[ActivatedEffectRule(Priority = 81)]
public sealed class TargetCreatureMustBeBlockedThisTurnEffectRule : IActivatedEffectRule
{
  // "target <filter> must be blocked this turn if able"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+must\s+be\s+blocked\s+this\s+turn\s+if\s+able$",
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

    return new MustBeBlockedEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
