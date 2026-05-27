namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Cost increase effect: spells matching the containing ability's
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/> filter
/// cost more to cast (Rule 601.2f — total cost modification).
/// "Noncreature spells cost {1} more to cast." (Thorn of Amethyst / Sphere of
/// Resistance pattern). The filter sits on the enclosing
/// <see cref="MagicAST.AST.Abilities.StaticAbility"/>; this effect carries only
/// the amount of the increase.
/// </summary>
[OracleEffect("costIncrease")]
public sealed record CostIncreaseEffect : Effect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The amount of the increase.
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
