namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "All creatures able to block [Target] do so." — Lure-type forcing requirement.
/// Rule 509.1c (block requirements that the defending player must satisfy when possible).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect records that every creature that can legally block
/// the named object is required to do so; it does not model the runtime legality
/// check or the declare-blockers enforcement.
///
/// <para>
/// This is distinct from the two sibling effects:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <see cref="MustBeBlockedEffect"/> — attacker-side block requirement:
///     "this creature must be blocked if able." A specific creature must receive
///     at least one blocker. Does NOT force all potential blockers; one is enough.
///   </description></item>
///   <item><description>
///     <see cref="MustBlockEffect"/> — blocker-side requirement:
///     "this creature blocks if able." A specific creature must be declared
///     as a blocker whenever it legally can.
///   </description></item>
///   <item><description>
///     <see cref="AllMustBlockEffect"/> — universal blocker-side requirement
///     scoped to a named attacker: every creature that is able to block the
///     named object must do so. This is the Lure family (Rule 509.1c); the
///     defending player cannot choose to hold back blockers.
///   </description></item>
/// </list>
///
/// <para>
/// Three oracle-text shapes map to this effect:
/// <list type="bullet">
///   <item><c>All creatures able to block this creature do so.</c>
///         — <c>Target: Self</c>; static ability on the creature itself.</item>
///   <item><c>All creatures able to block enchanted creature do so.</c>
///         — <c>Target: EnchantedOrEquipped</c>; static ability on an Aura.</item>
///   <item><c>All creatures able to block target creature this turn do so.</c>
///         — <c>Target: Target/creature</c> with <c>Duration: UntilEndOfTurn</c>;
///           one-shot spell effect.</item>
/// </list>
/// </para>
/// </remarks>
[OracleEffect("allMustBlock")]
public sealed record AllMustBlockEffect : ContinuousEffect
{
  /// <summary>
  /// The creature that all able blockers must block.
  /// <c>Self</c> for static-ability-on-creature shape;
  /// <c>EnchantedOrEquipped</c> for Aura shape;
  /// <c>Target/creature</c> for the spell ("target creature") shape.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
