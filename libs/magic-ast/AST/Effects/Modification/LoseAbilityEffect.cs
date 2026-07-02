namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[target] loses [ability]"
/// </summary>
[OracleEffect("loseAbility")]
public sealed record LoseAbilityEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The ability text that is lost, or "all abilities"
  /// </summary>
  [FreeTextField]
  public required string AbilityText { get; init; }
}
