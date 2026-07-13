namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Triggered-ability effect clause "another target attacking creature can't be
/// blocked this turn." — the resolution half of an attack trigger that makes a
/// chosen attacking creature (other than the source) unblockable for the turn.
/// Covers Clammy Prowler: "Whenever this creature attacks, another target
/// attacking creature can't be blocked this turn." The trigger side is owned by
/// <see cref="AttacksConditionRule"/> (CR 508.1 — declare attackers); this rule
/// handles the effect clause only (CR 603.1).
///
/// <para>
/// Emits a <see cref="CantBeBlockedEffect"/> — attacker-side evasion (CR 509.1b:
/// "effects that say a creature can't block") — with an <c>untilEndOfTurn</c>
/// duration ("this turn"). The target is <see cref="ObjectReferenceKind.Target"/>
/// whose filter carries two axes beyond the bare "target creature can't be blocked
/// this turn" shape (<see cref="MagicAST.Parsing.Parsers.Spell.Rules.TargetCantBeBlockedThisTurnRule"/>):
/// <c>ExcludeSelf = true</c> ("another" — the codebase convention mapping "another"
/// to <see cref="ObjectFilter.ExcludeSelf"/>, CR 109.5) and the combat-state
/// predicate <c>CombatState.Attacking</c> ("attacking creature"), mirroring how
/// <c>ModifyPTTargetAttackingCreatureEffectRule</c> encodes the attacking filter.
/// </para>
///
/// <para>
/// Fully anchored (^ … $) and prefixed with "another" so it can never substring
/// the sibling <c>^target … can't be blocked</c> / <c>^target … can't block</c>
/// surfaces, and vice versa.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class AnotherTargetAttackingCreatureCantBeBlockedThisTurnRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^another\s+target\s+attacking\s+creature\s+can'?t\s+be\s+blocked\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CantBeBlockedEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          ExcludeSelf = true,
          Characteristics = [Characteristic.InCombat(CombatState.Attacking)],
        },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
