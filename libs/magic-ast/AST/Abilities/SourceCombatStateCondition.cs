namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "this [permanent] is attacking" / "this [permanent] is blocking" — a game-state
/// predicate on the <em>source</em> object's current combat role (CR 508 / 509).
///
/// <para>
/// Models the "Activate only if this creature is attacking" restriction family
/// (Glint-Horn Buccaneer, Spectral Sailor, Boltbender). The <see cref="State"/>
/// field carries the required combat role; <see cref="CombatState.Attacking"/> is
/// the dominant form. "This creature" is always the ability's own source (Self);
/// no reference field is needed.
/// </para>
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.References.CombatStateCharacteristic"/>
/// (an <see cref="MagicAST.AST.References.ObjectFilter"/> predicate that selects
/// <em>other</em> objects in a given combat role): this is a
/// <see cref="Condition"/> on the source object itself, not a filter axis.
/// Reference-not-resolution (ADR 0004): MAST records which state is required;
/// the engine evaluates it against the actual combat stack at activation time.
/// CR 508.1 (Declare Attackers step) / CR 602.5c (activation restrictions).
/// </para>
/// </summary>
[ConditionKind("sourceCombatState")]
public sealed record SourceCombatStateCondition : Condition
{
  /// <summary>
  /// The combat role the source object must be in — <see cref="CombatState.Attacking"/>
  /// for "this creature is attacking", <see cref="CombatState.Blocking"/> for "this
  /// creature is blocking", etc.
  /// </summary>
  public required CombatState State { get; init; }
}
