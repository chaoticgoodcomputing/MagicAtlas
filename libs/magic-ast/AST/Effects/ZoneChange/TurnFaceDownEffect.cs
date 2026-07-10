namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Turn this enchantment face down" / "Turn this permanent face down" — an
/// instructed action that flips a face-up permanent to face down (Obscuring
/// Aether: "{1}{G}: Turn this enchantment face down. (It becomes a 2/2
/// creature.)").
///
/// <para>
/// CR 708.1: "Some cards allow spells and permanents to be face down." CR 708.2a:
/// "If a face-up permanent is turned face down by a spell or ability that doesn't
/// list any characteristics for that object, it becomes a 2/2 face-down creature
/// with no text, no name, no subtypes, and no mana cost. ... These values are the
/// copiable values of that object's characteristics." MAST records the instructed
/// action and the subject; the resulting 2/2 no-name/no-text default characteristics
/// (the printed reminder-text parenthetical) are engine territory per the
/// descriptive-not-engine doctrine.
/// </para>
///
/// <para>
/// Sibling of <see cref="TransformEffect"/> (same "flip the permanent's visible
/// state" shape) but distinct game action: CR 701.27b notes transforming and
/// turning face up/down "uses the same physical action" but "are different game
/// actions" — abilities that trigger on one don't trigger on the other.
/// </para>
/// </summary>
[OracleEffect("turnFaceDown")]
public sealed record TurnFaceDownEffect : Effect
{
  /// <summary>
  /// The permanent being turned face down. Typically <see cref="ObjectReference.Self()"/>
  /// for "turn this [permanent] face down".
  /// </summary>
  public required ObjectReference Target { get; init; }
}
