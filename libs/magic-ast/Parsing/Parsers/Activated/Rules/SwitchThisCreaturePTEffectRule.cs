namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Activated-ability effect shape "Switch this creature's power and toughness until
/// end of turn." — e.g. Myr Quadropod: "{3}: Switch this creature's power and
/// toughness until end of turn."
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are written as
/// \"[Cost]: [Effect.] [Activation instructions (if any).]\"" The cost container
/// ({3}) is parsed independently by <see cref="ActivatedAbilityParser"/>; this rule
/// only recognizes the post-colon effect body.
///
/// CR 613.4d (Layer 7d): "Effects that switch a creature's power and toughness are
/// applied. Such effects take the value of power and apply it to the creature's
/// toughness, and take the value of toughness and apply it to the creature's power."
///
/// Self ("this creature") counterpart of the spell-side
/// <c>SwitchTargetCreaturePTSpellRule</c> ("Switch target creature's power and
/// toughness ..."). Emits the same <see cref="SwitchPTEffect"/>, but with the source
/// creature (<see cref="ObjectReferenceKind.Self"/>) as its target. Fully anchored so
/// it cannot capture a sibling effect fragment.
/// </summary>
[ActivatedEffectRule(Priority = 90)]
public sealed class SwitchThisCreaturePTEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Switch\s+this\s+creature's\s+power\s+and\s+toughness\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new SwitchPTEffect
    {
      Target = ObjectReference.Self(),
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
