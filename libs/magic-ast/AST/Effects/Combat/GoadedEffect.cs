namespace MagicAST.AST.Effects.Combat;

using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[target] is goaded." CR 701.15b (verbatim): "Goaded is a designation a
/// permanent can have. A goaded creature attacks each combat if able and
/// attacks a player other than the controller of the permanent, spell, or
/// ability that caused it to be goaded if able. Goaded is neither an ability
/// nor part of the permanent's copiable values."
/// </summary>
/// <remarks>
/// MAST records the goaded designation as a continuous fact assigned to
/// <see cref="Target"/> — the descriptive "is goaded" instruction, not the
/// downstream combat rules it triggers. The attacks-each-combat-if-able and
/// attacks-a-player-other-than requirements that CR 701.15b spells out are
/// consequences of holding the designation, not separate MAST effects; they
/// are engine territory (descriptive-not-executive doctrine, ADR 0001) —
/// mirrors how <see cref="MagicAST.AST.Effects.Timing.BecomeMonarchEffect"/>
/// records the monarch designation without modeling its draw/combat-transfer
/// rules.
///
/// <para>
/// Typical <see cref="Target"/> is an <c>EnchantedOrEquipped</c>
/// <see cref="ObjectReference"/> for a static grant printed on an Equipment
/// or Aura body (e.g. "Equipped creature gets +2/+0 and is goaded."); the
/// same shape covers one-shot "goad target creature" instructions with a
/// <c>Target</c> <see cref="ObjectReference"/>.
/// </para>
/// </remarks>
[OracleEffect("goaded")]
public sealed record GoadedEffect : ContinuousEffect
{
  /// <summary>
  /// The object that receives the goaded designation.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
