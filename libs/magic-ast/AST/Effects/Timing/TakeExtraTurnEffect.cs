namespace MagicAST.AST.Effects.Timing;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using System.Text.Json.Serialization;

/// <summary>
/// "Take an extra turn after this one." / "Target player takes an extra turn after
/// this one." — schedules an additional full turn for the specified player to be
/// taken immediately after the current turn resolves.
///
/// <para>
/// CR 500.7 (verbatim): "Some effects can give a player extra turns. They do this
/// by adding the turns directly after the specified turn. If a player is given
/// multiple extra turns, the extra turns are added one at a time. If multiple
/// players are given extra turns, the extra turns are added one at a time, in
/// APNAP order (see rule 101.4). The most recently created turn will be taken first."
/// </para>
///
/// <para>
/// MAST records the verb + the player reference; the turn-ordering bookkeeping
/// and interaction with skip-turn effects (CR 500.7) are engine territory,
/// per the descriptive-not-engine doctrine (ADR 0001).
/// </para>
/// </summary>
[OracleEffect("takeExtraTurn")]
public sealed record TakeExtraTurnEffect : Effect
{
  /// <summary>
  /// Which player takes the extra turn. Defaults to
  /// <see cref="ObjectReference.You"/> (the controller of the ability) for
  /// the canonical "Take an extra turn after this one." oracle form. Use
  /// a <see cref="ObjectReferenceKind.Target"/> reference for "Target player
  /// takes an extra turn after this one." (Timetwister-adjacent spells).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
