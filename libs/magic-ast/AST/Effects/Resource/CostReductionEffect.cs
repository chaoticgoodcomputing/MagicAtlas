namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Cost reduction effect: reduces the cost to cast this spell.
/// "This spell costs {X} less to cast..." where X is determined by some condition.
/// </summary>
[OracleEffect("costReduction")]
public sealed record CostReductionEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
