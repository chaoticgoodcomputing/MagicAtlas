namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// A restriction on what can be targeted when casting a spell or activating an ability.
/// e.g., "You can't choose an untapped creature as this spell's target"
/// </summary>
[OracleEffect("targetingRestriction")]
public sealed record TargetingRestrictionEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The type of restriction: cantTarget, cantTargetUnless, mustTarget, etc.
  /// </summary>
  public required string Restriction { get; init; }

  /// <summary>
  /// The condition that must be met (or not met) for targeting.
  /// </summary>
  public required TargetingCondition Condition { get; init; }

  /// <summary>
  /// When this restriction applies: casting, activating, always.
  /// </summary>
  public required string AppliesWhen { get; init; }

  /// <summary>
  /// What is being targeted.
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
