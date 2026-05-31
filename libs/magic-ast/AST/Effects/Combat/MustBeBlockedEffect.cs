namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block requirement: oracle text states that a creature "must be blocked if able."
/// Rule 509.1c (the defending player's declared blockers must satisfy block requirements when possible).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's oracle line
/// imposes a "must be blocked if able" requirement on the named object; it does not model
/// the runtime decision that the defending player must make at declare-blockers.
///
/// This is the dual of <see cref="MustAttackEffect"/> — that node records a Rule 508.1d
/// attack requirement; this one records a Rule 509.1c block requirement. They are different
/// requirements on different combat steps and intentionally separate nodes (the briefing's
/// anti-pattern: do not conflate with <c>EvasionEffect</c>, which is a *restriction* on what
/// can block, not a *requirement* that something must).
///
/// Typical Target is the card itself (an <c>ObjectReference</c> with kind <c>Self</c>),
/// but the same shape covers lines like "Creatures you control must be blocked if able"
/// where the target is an <c>ObjectFilter</c>-shaped reference.
/// </remarks>
[OracleEffect("mustBeBlocked")]
public sealed record MustBeBlockedEffect : ContinuousEffect
{
  /// <summary>
  /// The object the requirement applies to. Usually <c>Self</c> for a creature whose
  /// own oracle line says "[Self] must be blocked if able."
  /// </summary>
  public required ObjectReference Target { get; init; }
}
