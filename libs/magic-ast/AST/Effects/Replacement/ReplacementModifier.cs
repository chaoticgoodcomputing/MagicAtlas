namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Modifier for replacement effects that scale the original event.
/// </summary>
public sealed record ReplacementModifier
{
  /// <summary>
  /// The type of modification: "double", "triple", "plusOne", "plusX", etc.
  /// </summary>
  [JsonPropertyName("type")]
  public required string Type { get; init; }

  /// <summary>
  /// For variable modifiers, the amount.
  /// </summary>
  [JsonPropertyName("amount")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }
}
