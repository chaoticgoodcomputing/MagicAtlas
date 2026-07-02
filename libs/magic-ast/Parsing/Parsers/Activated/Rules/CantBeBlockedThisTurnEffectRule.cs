namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// "This creature can't be blocked this turn." / "Target creature can't be
/// blocked this turn." — activated ability effect that grants full
/// unblockability until end of turn.
///
/// CR 509.1b: "The defending player checks each creature they control to see
/// whether it's affected by any restrictions (effects that say a creature can't
/// block, or that it can't block unless some condition is met)." A "can't be
/// blocked" effect is a blocking restriction on the attacker.
///
/// Maps to <see cref="CantBeBlockedEffect"/> with:
/// <list type="bullet">
///   <item><description><see cref="CantBeBlockedEffect.Target"/> = <c>Self</c> for
///   the "This creature" form — the creature that pays the cost is the one that
///   becomes unblockable; or a <c>Target</c> creature reference for the "Target
///   creature" form (Whirler Rogue, CR 115.1).</description></item>
///   <item><description><see cref="CantBeBlockedEffect.Duration"/> = <c>untilEndOfTurn</c>
///   — oracle phrase "this turn" maps to end-of-turn expiry.</description></item>
///   <item><description>No <see cref="CantBeBlockedEffect.BlockedByFilter"/> — the
///   restriction is unconditional.</description></item>
/// </list>
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class CantBeBlockedThisTurnEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    if (trimmed.Equals(
          "This creature can't be blocked this turn",
          System.StringComparison.OrdinalIgnoreCase))
    {
      return new CantBeBlockedEffect
      {
        Target = ObjectReference.Self(),
        Duration = UntilTimeDuration.EndOfTurn,
      };
    }

    if (trimmed.Equals(
          "Target creature can't be blocked this turn",
          System.StringComparison.OrdinalIgnoreCase))
    {
      return new CantBeBlockedEffect
      {
        Target = ObjectReference.Target(new ObjectFilter { CardTypes = ["creature"] }),
        Duration = UntilTimeDuration.EndOfTurn,
      };
    }

    return null;
  }
}
