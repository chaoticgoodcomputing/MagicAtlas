namespace MagicAST.AST.Effects.Damage;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Damage that would be dealt by this creature can't be prevented." (Excruciator) —
/// a source-scoped prevention-lock static that nullifies every damage-prevention
/// effect (CR 615.1: prevention effects) applicable to damage dealt BY the named
/// <see cref="Source"/>, while leaving prevention of damage from other sources
/// unaffected. Written as a plain static statement (CR 604.1: "Static abilities do
/// something all the time rather than being activated or triggered. They are
/// written as statements, and they're simply true."), it is a rules-of-the-game-
/// modifying continuous effect (CR 611.1) — the source-scoped sibling of
/// <see cref="CantPreventDamageEffect"/> (Leyline of Punishment's unconditional,
/// global "Damage can't be prevented."): where that marker carries no fields
/// because no source/recipient is named, this effect names exactly one damage
/// source (<see cref="ObjectReferenceKind.Self"/> for "this creature") and locks
/// prevention only for damage dealt by it.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not the replacement/prevention-effect
/// application machinery that CR 615 defines. This effect records only that damage
/// prevention is locked out for damage dealt by <see cref="Source"/>; it does NOT
/// model how the game engine would suppress a live prevention shield.
/// </remarks>
[OracleEffect("damageCantBePrevented")]
public sealed record DamageCantBePreventedEffect : Effect
{
  /// <summary>
  /// The source whose damage can't be prevented — "damage that would be dealt by
  /// [this creature]" (<see cref="ObjectReferenceKind.Self"/> for Excruciator).
  /// </summary>
  public required ObjectReference Source { get; init; }
}
