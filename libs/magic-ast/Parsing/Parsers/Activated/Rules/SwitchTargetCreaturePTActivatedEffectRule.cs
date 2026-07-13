namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Switch target creature's power and toughness until end of turn." — the P/T-switch
/// shape as an activated-ability effect (Dwarven Thaumaturgist's <c>{T}</c> ability).
///
/// CR 613.4 (Layer 7d): "Effects that switch a creature's power and toughness are
/// applied. Such effects take the value of power and apply it to the creature's
/// toughness, and take the value of toughness and apply it to the creature's power."
///
/// The activated-ability counterpart of the instant-speed
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.SwitchTargetCreaturePTSpellRule"/>
/// (Twisted Image). Both emit the shared <see cref="SwitchPTEffect"/> node.
///
/// GUARD: fully anchored (<c>^…$</c>) to exactly the switch-P/T sentence with an
/// optional trailing period; no sibling activated effect shares this surface phrase,
/// so the anchored matcher is disjoint from every other rule.
/// </summary>
[ActivatedEffectRule(Priority = 500)]
public sealed class SwitchTargetCreaturePTActivatedEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Switch\s+target\s+creature's\s+power\s+and\s+toughness\s+until\s+end\s+of\s+turn\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    if (!Pattern.IsMatch(effectText.Trim()))
    {
      return null;
    }

    return new SwitchPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
