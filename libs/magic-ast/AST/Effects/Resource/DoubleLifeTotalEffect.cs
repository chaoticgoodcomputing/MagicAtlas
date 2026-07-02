namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Double [player]'s life total." — a one-shot life-doubling effect.
///
/// <para>
/// Rule 701.10d: "To double a player's life total, the player gains or loses an
/// amount of life such that their new life total is twice its current value."
/// MAST records what the oracle text says ("double … life total"), not the
/// gain/lose arithmetic the engine performs at resolution.
/// </para>
///
/// <para>
/// Canonical use: Beacon of Immortality — "Double target player's life total."
/// The player is always a targeted player in this context.
/// </para>
/// </summary>
[OracleEffect("doubleLifeTotal")]
public sealed record DoubleLifeTotalEffect : Effect
{
  /// <summary>
  /// The player whose life total is doubled. Typically
  /// <see cref="ObjectReferenceKind.Target"/> (a targeted player) or
  /// <see cref="ObjectReferenceKind.You"/> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
