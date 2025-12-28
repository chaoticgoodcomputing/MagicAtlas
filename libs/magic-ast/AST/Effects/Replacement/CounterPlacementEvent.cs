namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Counter placement event: "counters would be put on"
/// </summary>
public sealed record CounterPlacementEvent : ReplacementEvent
{
  /// <summary>
  /// Type of counter (e.g., "+1/+1", "loyalty", or null for any).
  /// </summary>
  [JsonPropertyName("counterType")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CounterType { get; init; }
}
