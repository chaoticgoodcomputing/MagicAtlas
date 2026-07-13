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
  public required ProtectionQualityKind Kind { get; init; }

  /// <summary>
  /// The specific value (e.g., "red", "Demon", "artifact").
  /// Null for kind = "everything".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Value { get; init; }

  /// <summary>
  /// When <see cref="Kind"/> is <see cref="ProtectionQualityKind.ChosenCharacteristic"/>,
  /// which earlier-bound characteristic axis this quality refers back to (CR 607
  /// linked ability) — e.g. <see cref="ChosenCharacteristicKind.Color"/> for
  /// "protection from the chosen color". Mirrors
  /// <see cref="ObjectFilter.ChosenCharacteristic"/>'s object-reference analogue.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ChosenCharacteristicKind? ChosenCharacteristic { get; init; }
}
