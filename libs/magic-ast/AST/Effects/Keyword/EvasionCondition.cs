namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Condition for evasion (used by landwalk and similar effects).
/// </summary>
public sealed record EvasionCondition
{
  /// <summary>
  /// The type of condition check.
  /// </summary>
  [JsonPropertyName("conditionType")]
  public required EvasionConditionType ConditionType { get; init; }

  /// <summary>
  /// For "defendingPlayerControls": what they must control.
  /// </summary>
  [JsonPropertyName("permanentFilter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? PermanentFilter { get; init; }
}
