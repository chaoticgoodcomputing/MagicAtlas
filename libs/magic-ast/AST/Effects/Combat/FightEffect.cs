namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Fight keyword action (CR 701.14): "Target creature you control fights target
/// creature you don't control."
///
/// MAST records the two participants as target references and does not model the
/// mutual-damage exchange — that is engine territory per the descriptive-not-engine
/// doctrine. Each creature deals damage equal to its power to the other; MAST
/// describes <em>who fights whom</em>, not the resulting damage events.
/// </summary>
/// <remarks>
/// <para>
/// CR 701.14a: "A spell or ability may instruct a creature to fight another
/// creature or instruct two creatures to fight each other. Each of those creatures
/// deals damage equal to its power to the other."
/// </para>
/// <para>
/// <see cref="Controller"/> filter convention:
/// <list type="bullet">
///   <item><description>
///     <see cref="Controlled"/> — always <c>Controller: You</c>; the caster's creature.
///   </description></item>
///   <item><description>
///     <see cref="Opposed"/> — <c>Controller: Opponent</c> for both
///     "you don't control" and "an opponent controls" oracle phrasings.
///     The Comprehensive Rules treat these as equivalent for targeting purposes.
///   </description></item>
/// </list>
/// </para>
/// <para>
/// Reminder text "(Each deals damage equal to its power to the other.)" is stripped
/// by the parenthetical-removal pass before spell rules are evaluated.
/// </para>
/// </remarks>
[OracleEffect("fight")]
public sealed record FightEffect : Effect
{
  /// <summary>
  /// The creature the caster controls that participates in the fight.
  /// Typically: target creature you control.
  /// </summary>
  public required ObjectReference Controlled { get; init; }

  /// <summary>
  /// The creature the caster does not control (opponent's creature) that
  /// participates in the fight. Covers both "you don't control" and
  /// "an opponent controls" phrasings.
  /// </summary>
  public required ObjectReference Opposed { get; init; }
}
