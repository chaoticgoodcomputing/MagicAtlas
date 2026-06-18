namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Sets a player's life total to a specific number: "[player's] life total becomes [N]."
/// Rule 119.5: "If an effect sets a player's life total to a specific number, the player
/// gains or loses the necessary amount of life to end up with the new total."
///
/// <para>
/// MAST describes what the oracle text says, not what the rules engine enforces. The
/// presence of this effect records that the card sets a player's life total to a fixed
/// value; it does not model the gain/lose calculation the engine performs.
/// </para>
///
/// <para>
/// Canonical use: Master of Cruelties — "that player's life total becomes 1" when this
/// creature attacks a player and isn't blocked. The effect is unconditional once the
/// triggered ability resolves; the conditional nature (attacks a player, isn't blocked)
/// lives in the <see cref="MagicAST.AST.Abilities.TriggeredAbility.InterveningIf"/>
/// on the parent ability.
/// </para>
/// </summary>
[OracleEffect("setLifeTotal")]
public sealed record SetLifeTotalEffect : Effect
{
  /// <summary>
  /// The player whose life total is set. Typically
  /// <see cref="ObjectReferenceKind.ThatPlayer"/> (the player the trigger refers to),
  /// <see cref="ObjectReferenceKind.Target"/>, or <see cref="ObjectReferenceKind.You"/>.
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The fixed life-total value the player's life total becomes.
  /// Rule 119.5: the player gains or loses the amount needed to reach this number.
  /// </summary>
  public required int Total { get; init; }
}
