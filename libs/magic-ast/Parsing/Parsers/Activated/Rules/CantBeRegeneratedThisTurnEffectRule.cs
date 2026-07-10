namespace MagicAST.Parsing.Parsers.Activated.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;

/// <summary>
/// "This creature can't be regenerated this turn." — activated ability effect
/// that shuts off regeneration shields for the object bearing the ability
/// through the rest of the current turn (Clergy of the Holy Nimbus:
/// "{1}: This creature can't be regenerated this turn. Only your opponents may
/// activate this ability.").
///
/// CR 701.19 (regenerate): the effect denies the "next time this creature would
/// be destroyed this turn" shield outright, rather than modifying a specific
/// resolving destroy effect (contrast the <c>CantBeRegenerated</c> rider carried
/// on <see cref="MagicAST.AST.Effects.ZoneChange.DestroyEffect"/> for "Destroy
/// target creature. It can't be regenerated.").
///
/// Maps to <see cref="CantBeRegeneratedEffect"/> with <c>Target</c> left null
/// (means the ability's controlling object — same nullable-Target-means-Self
/// convention as <see cref="MagicAST.AST.Effects.Combat.CantAttackEffect.Target"/>)
/// and <c>Duration</c> = end of turn, matching the oracle phrase "this turn"
/// (same pattern as the sibling turn-scoped restrictions <c>CantAttackThisTurnEffectRule</c>/
/// <c>CantBlockThisTurnEffectRule</c>/<c>CantBeBlockedThisTurnEffectRule</c>).
/// </summary>
[ActivatedEffectRule(Priority = 80)]
public sealed class CantBeRegeneratedThisTurnEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    if (
      trimmed.Equals(
        "This creature can't be regenerated this turn",
        System.StringComparison.OrdinalIgnoreCase
      )
    )
    {
      return new CantBeRegeneratedEffect { Duration = UntilTimeDuration.EndOfTurn };
    }

    return null;
  }
}
