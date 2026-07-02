namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Counter placement event: "counters would be put on"
/// </summary>
[OracleReplacementEvent("counterPlacement")]
public sealed record CounterPlacementEvent : ReplacementEvent
{
  /// <summary>
  /// Type of counter (e.g., "+1/+1", "loyalty", or null for any).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CounterType { get; init; }

  /// <summary>
  /// Minimum quantity for the event to apply (e.g., "one or more" = 1).
  /// Mirrors <see cref="TokenCreationEvent.MinimumQuantity"/> for parity.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumQuantity { get; init; }
}
