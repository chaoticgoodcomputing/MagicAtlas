namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Turn target face-down creature face up" / "Turn this creature face up" — an
/// instructed action that flips a face-down permanent to face up (Ixidor, Reality
/// Sculptor: "{2}{U}: Turn target face-down creature face up."). The permanent
/// returns to its face-up characteristics (CR 708.4).
///
/// <para>
/// CR 708.1: "Some cards allow spells and permanents to be face down." CR 708.4:
/// "As a permanent is turned face up, its characteristics ... are revealed." Cards
/// with Morph, Disguise, or Manifest supply the "turn face up" action via their own
/// keyword (paying the morph cost); this node models the DIRECT "turn target
/// face-down creature face up" instruction that appears as a standalone effect and
/// does not itself pay any face-up cost. MAST records the instructed action and the
/// subject; the revealed characteristics are engine territory.
/// </para>
///
/// <para>
/// Mirror of <see cref="TurnFaceDownEffect"/> (same "flip the permanent's visible
/// state" shape, opposite direction) and a sibling of <see cref="TransformEffect"/>:
/// CR 701.27b notes transforming and turning face up/down "uses the same physical
/// action" but "are different game actions" — abilities that trigger on one don't
/// trigger on the other.
/// </para>
/// </summary>
[OracleEffect("turnFaceUp")]
public sealed record TurnFaceUpEffect : Effect
{
  /// <summary>
  /// The permanent being turned face up. Typically a targeted face-down creature
  /// (<see cref="ObjectReferenceKind.Target"/> with an <c>IsFaceDown</c> filter),
  /// or <see cref="ObjectReference.Self()"/> for "turn this [permanent] face up".
  /// </summary>
  public required ObjectReference Target { get; init; }
}
