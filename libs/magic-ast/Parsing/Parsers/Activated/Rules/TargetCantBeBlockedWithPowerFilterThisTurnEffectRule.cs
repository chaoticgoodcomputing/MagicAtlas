namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "Target creature with power N or less/greater can't be blocked this turn." —
/// activated ability effect that grants full unblockability until end of turn
/// to a target creature constrained by a power comparison filter.
///
/// CR 509.1b: "The defending player checks each creature they control to see
/// whether it's affected by any restrictions (effects that say a creature can't
/// block, or that it can't block unless some condition is met)." A "can't be
/// blocked" effect is a blocking restriction on the attacker — an evasion
/// ability under CR 509.1 (a static ability an attacking creature has that
/// restricts what can block it).
///
/// CR 602.5: "A player can't begin to activate an ability that's prohibited
/// from being activated." Governs the {T} activation cost (Goblin Tunneler).
///
/// Maps to <see cref="CantBeBlockedEffect"/> with:
/// <list type="bullet">
///   <item><description><see cref="CantBeBlockedEffect.Target"/> = a
///   <c>Target</c> creature reference filtered by
///   <see cref="ObjectFilter.PowerComparison"/> (Goblin Tunneler,
///   "power 2 or less").</description></item>
///   <item><description><see cref="CantBeBlockedEffect.Duration"/> = <c>untilEndOfTurn</c>
///   — oracle phrase "this turn" maps to end-of-turn expiry.</description></item>
///   <item><description>No <see cref="CantBeBlockedEffect.BlockedByFilter"/> — the
///   restriction is unconditional (full unblockability), mirroring
///   <see cref="CantBeBlockedThisTurnEffectRule"/>.</description></item>
/// </list>
///
/// Power-filter parsing mirrors
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.DestroyTargetWithFilterRule.PowerPattern"/>:
/// "power N or greater" maps to <see cref="ComparisonOperator.GreaterThanOrEqual"/>,
/// "power N or less" maps to <see cref="ComparisonOperator.LessThanOrEqual"/>.
///
/// Sits above (higher priority than) the generic, unfiltered
/// <see cref="CantBeBlockedThisTurnEffectRule"/> (Priority 80) — collision-free,
/// since that rule's exact-string match cannot claim this power-filtered phrase.
/// </summary>
[ActivatedEffectRule(Priority = 81)]
public sealed class TargetCantBeBlockedWithPowerFilterThisTurnEffectRule : IActivatedEffectRule
{
  // Anchored end-to-end so this rule only claims the specific power-filtered
  // "can't be blocked" phrase and never shadows other activated effects.
  private static readonly Regex PowerFilterPattern = new(
    @"^Target\s+creature\s+with\s+power\s+(?<n>\d+)\s+or\s+(?<dir>less|greater)\s+can't\s+be\s+blocked\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    var match = PowerFilterPattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var value = int.Parse(match.Groups["n"].Value);
    var op = match.Groups["dir"].Value.Equals("greater", System.StringComparison.OrdinalIgnoreCase)
      ? ComparisonOperator.GreaterThanOrEqual
      : ComparisonOperator.LessThanOrEqual;

    return new CantBeBlockedEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          PowerComparison = new Comparison { Operator = op, Value = value },
        },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
