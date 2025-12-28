namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Cost reduction effect: reduces the cost to cast this spell.
/// "This spell costs {X} less to cast..." where X is determined by some condition.
/// </summary>
public sealed record CostReductionEffect : Effect
{
  /// <summary>
  /// The amount of the reduction.
  /// </summary>
  [JsonPropertyName("amount")]
  public required Quantity Amount { get; init; }

  /// <summary>
  /// What the reduction scales with (e.g., "noncombat damage dealt to your opponents this turn").
  /// </summary>
  [JsonPropertyName("basedOn")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? BasedOn { get; init; }

  /// <summary>
  /// What this reduction is "for each" of.
  /// e.g., "for each creature you control"
  /// </summary>
  [JsonPropertyName("perObject")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? PerObject { get; init; }

  /// <summary>
  /// Optional condition for when the reduction applies.
  /// </summary>
  [JsonPropertyName("condition")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Condition { get; init; }
}
