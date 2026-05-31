namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Combat-block requirement (blocker-side): oracle text states that a creature
/// "blocks each combat if able" or "must block if able."
/// Rule 509.1c (the defending player's declared blockers must satisfy any
/// block requirements that apply to their creatures when possible).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's
/// oracle line imposes a "must block" requirement on the named object; it does
/// not model the runtime decision that the defending player must make at
/// declare-blockers.
///
/// <para>
/// This is the dual of <see cref="MustBeBlockedEffect"/> — same rule (509.1c),
/// different side of the combat-requirement relationship:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustBeBlockedEffect"/> is an <i>attacker-side</i> requirement:
///     "this creature must be blocked if able" places a requirement on the
///     defending player's block declaration concerning a specific attacker.
///   </description></item>
///   <item><description>
///     <see cref="MustBlockEffect"/> is a <i>blocker-side</i> requirement:
///     "this creature blocks if able" places a requirement on the defending
///     player's block declaration concerning a specific defender — i.e., the
///     listed creature must be declared as a blocker when it legally can.
///   </description></item>
/// </list>
/// <para>
/// They are distinct requirements on distinct objects, even though both cite
/// Rule 509.1c. Conflating them is a judge-FAIL anti-pattern.
/// </para>
/// <para>
/// Distinct from <see cref="MustAttackEffect"/> (Rule 508.1d, attack
/// requirement on the active player's attack declaration) — same shape, but a
/// different combat step and a different player's decision.
/// </para>
/// <para>
/// Typical Target is the card itself (an <c>ObjectReference</c> with kind
/// <c>Self</c>) for lines like "[Self] blocks each combat if able." Lines like
/// "All creatures block each combat if able" use an <c>ObjectReference</c>
/// with kind <c>Each</c> and a creature filter.
/// </para>
/// </remarks>
[OracleEffect("mustBlock")]
public sealed record MustBlockEffect : ContinuousEffect
{
  /// <summary>
  /// The object the requirement applies to. <c>Self</c> for a creature whose
  /// own oracle line says "[Self] blocks each combat if able"; an
  /// <c>Each</c>-kinded reference with a creature filter for global lines like
  /// "All creatures block each combat if able."
  /// </summary>
  public required ObjectReference Target { get; init; }
}
