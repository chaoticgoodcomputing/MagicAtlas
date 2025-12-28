namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// A condition for targeting restrictions.
/// </summary>
public sealed record TargetingCondition
{
  /// <summary>
  /// The characteristic being checked (e.g., "tapped", "attacking").
  /// </summary>
  [JsonPropertyName("characteristic")]
  public required string Characteristic { get; init; }

  /// <summary>
  /// The required value of the characteristic.
  /// </summary>
  [JsonPropertyName("value")]
  public required bool Value { get; init; }
}
