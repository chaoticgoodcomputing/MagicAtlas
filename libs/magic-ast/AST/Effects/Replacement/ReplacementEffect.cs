namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "If [event] would [happen], [alternative] instead"
/// Rule 614
/// </summary>
public sealed record ReplacementEffect : Effect
{
  /// <summary>
  /// The structured event being replaced/modified.
  /// </summary>
  [JsonPropertyName("event")]
  public required ReplacementEvent Event { get; init; }

  /// <summary>
  /// Whether the original event still occurs (true for augmentation like Chatterfang,
  /// false for pure replacement like "exile it instead").
  /// </summary>
  [JsonPropertyName("originalEventOccurs")]
  public bool OriginalEventOccurs { get; init; }

  /// <summary>
  /// The effect(s) that happen instead of or in addition to the original event.
  /// </summary>
  [JsonPropertyName("replacement")]
  public required Effect Replacement { get; init; }

  /// <summary>
  /// Optional modifier to the original event (e.g., "twice that many" for Doubling Season).
  /// </summary>
  [JsonPropertyName("modifier")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ReplacementModifier? Modifier { get; init; }
}
