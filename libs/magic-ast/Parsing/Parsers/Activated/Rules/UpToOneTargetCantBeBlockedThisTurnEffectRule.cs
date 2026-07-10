namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Up to one target creature can't be blocked this turn." (Key to the City) —
/// the "up to one" bounded-target variant of <see cref="CantBeBlockedThisTurnEffectRule"/>'s
/// "Target creature can't be blocked this turn." shape.
///
/// <para>
/// CR 509.1b: "The defending player checks each creature they control to see
/// whether it's affected by any restrictions (effects that say a creature
/// can't block, or that it can't block unless some condition is met)." A
/// "can't be blocked" effect is a blocking restriction on the attacker.
/// </para>
///
/// <para>
/// "Up to one" is modelled as <c>Quantity = UpToQuantity { Maximum = 1 }</c> on
/// the <see cref="ObjectReference"/> — the same convention as
/// <see cref="ReturnUpToOneTargetTypeDisjunctionToHandEffectRule"/> — because
/// the ability may be activated with no legal or chosen target (CR 601.2c /
/// targeting rules for "up to N"), not because the ability itself is optional.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class UpToOneTargetCantBeBlockedThisTurnEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    if (!trimmed.Equals(
          "Up to one target creature can't be blocked this turn",
          System.StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return new CantBeBlockedEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
        Quantity = new UpToQuantity { Maximum = 1, Minimum = 0 },
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
