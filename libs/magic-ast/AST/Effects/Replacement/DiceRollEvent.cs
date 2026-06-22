namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Die-roll event: "[a player] would roll one or more dice" (CR 706.1 — an effect
/// that instructs a player to roll a die specifies what kind of die and how many).
/// The replaceable event watched by the dice-advantage replacement effects (Pixie
/// Guide, Wyll, Blade of Frontiers, Barbarian Class): "If you would roll one or more
/// dice, instead roll that many dice plus one and ignore the lowest roll."
///
/// <para>
/// CR 614.1: such an effect is a replacement effect — it watches for the roll event
/// and replaces it with a modified roll, NOT a triggered ability. The rolling player
/// is carried on the inherited <see cref="ReplacementEvent.Controller"/>
/// (<see cref="MagicAST.AST.References.ObjectReference.You"/> for "If you would roll").
/// Mirrors <see cref="MillEvent"/>/<see cref="CounterPlacementEvent"/>: "one or more"
/// → <see cref="MinimumQuantity"/> 1.
/// </para>
/// </summary>
[OracleReplacementEvent("diceRoll")]
public sealed record DiceRollEvent : ReplacementEvent
{
  /// <summary>
  /// Minimum number of dice the roll must involve for the event to apply
  /// (e.g. "one or more dice" = 1). Mirrors <see cref="MillEvent.MinimumQuantity"/>
  /// and <see cref="CounterPlacementEvent.MinimumQuantity"/> for parity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumQuantity { get; init; }
}
