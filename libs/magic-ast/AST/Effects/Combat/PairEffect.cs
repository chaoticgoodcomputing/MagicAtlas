namespace MagicAST.AST.Effects.Combat;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "pair [this creature] with [another creature]" — the Soulbond pairing action
/// (CR 702.95): Soulbond is two triggered abilities meaning, in part, "you may pair
/// this creature with another unpaired creature you control for as long as both remain
/// creatures on the battlefield under your control."
///
/// <para>
/// Records only the pairing action and its partner reference. The shared abilities a
/// paired pair grants are expressed elsewhere over
/// <see cref="ObjectReferenceKind.BothPaired"/> (the consumer side, already present);
/// maintaining the pairing while control persists is engine territory.
/// </para>
/// </summary>
[OracleEffect("pair")]
public sealed record PairEffect : Effect
{
  /// <summary>
  /// The creature this one is paired with — Soulbond's "another unpaired creature
  /// you control".
  /// </summary>
  public required ObjectReference Partner { get; init; }
}
