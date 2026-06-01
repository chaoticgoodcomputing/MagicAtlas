namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mill event: "[a player] would mill one or more cards" (CR 701.17a — to mill a
/// number of cards is to put that many cards from the top of a library into a
/// graveyard). The replaceable event watched by effects such as Bruvac the
/// Grandiloquent. Mirrors <see cref="TokenCreationEvent"/>/<see cref="CounterPlacementEvent"/>:
/// the milling player is carried on the inherited <see cref="ReplacementEvent.Controller"/>
/// (e.g. <see cref="ObjectReference.Opponent"/> for "an opponent would mill").
/// </summary>
[OracleReplacementEvent("mill")]
public sealed record MillEvent : ReplacementEvent
{
  /// <summary>
  /// Minimum quantity for the event to apply (e.g., "one or more" = 1).
  /// Mirrors <see cref="TokenCreationEvent.MinimumQuantity"/> for parity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumQuantity { get; init; }
}
