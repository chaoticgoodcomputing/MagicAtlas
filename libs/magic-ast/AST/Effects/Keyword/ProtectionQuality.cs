namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// A quality that protection applies to.
/// </summary>
public sealed record ProtectionQuality
{
  /// <summary>
  /// The kind of quality: color, cardType, subtype, characteristic, or "everything".
  /// </summary>
  [JsonPropertyName("kind")]
  public required ProtectionQualityKind Kind { get; init; }

  /// <summary>
  /// The specific value (e.g., "red", "Demon", "artifact").
  /// Null for kind = "everything".
  /// </summary>
  [JsonPropertyName("value")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Value { get; init; }
}
