namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Token creation event: "one or more tokens would be created"
/// </summary>
public sealed record TokenCreationEvent : ReplacementEvent
{
  /// <summary>
  /// Filter for what kind of tokens (null = any tokens).
  /// </summary>
  [JsonPropertyName("tokenFilter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? TokenFilter { get; init; }

  /// <summary>
  /// Minimum quantity for the event to apply (e.g., "one or more" = 1).
  /// </summary>
  [JsonPropertyName("minimumQuantity")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumQuantity { get; init; }
}
