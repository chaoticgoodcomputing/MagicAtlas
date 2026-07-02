namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;

/// <summary>
/// Activated-ability "lure" effect shape "target [filter] blocks this
/// creature this turn if able." — e.g. Tangle Angler: "{G}: Target creature
/// blocks this creature this turn if able."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are
/// written as \"[Cost]: [Effect.] [Activation instructions (if any).]\"
/// Example: The activation cost of an ability that reads \"{2}, {T}: You
/// gain 1 life\" is two mana of any type plus tapping the permanent that has
/// the ability." The cost container ({G}) is parsed independently; this rule
/// only recognizes the post-colon effect body.
///
/// CR 509.1c: "The defending player checks each creature they control to
/// see whether it's affected by any requirements (effects that say a
/// creature must block, or that it must block if some condition is met). ...
/// If a requirement that says a creature blocks if able during a certain
/// turn refers to a turn with multiple combat phases, the creature blocks if
/// able during each declare blockers step in that turn." This is a
/// blocker-side requirement placed on the targeted creature (the forced
/// blocker), naming the activating creature (<c>Self</c>) as the attacker it
/// must block — the dual of an attacker-side "must be blocked" lure.
///
/// Maps to <see cref="MustBlockEffect"/> with <c>Target</c> = the targeted
/// forced blocker (parsed via <see cref="SpellRuleHelpers.ParseTargetFilter"/>),
/// <c>Blocks</c> = <see cref="ObjectReference.Self"/> (this creature), and
/// <c>Duration</c> = end of turn, matching the oracle phrase "this turn".
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class TargetCreatureBlocksThisCreatureThisTurnEffectRule : IActivatedEffectRule
{
  // "target <filter> blocks this creature this turn if able"
  private static readonly Regex Pattern = new(
    @"^target\s+(?<filter>.+?)\s+blocks\s+this\s+creature\s+this\s+turn\s+if\s+able$",
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

    return new MustBlockEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      Blocks = ObjectReference.Self(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
