namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[target] loses [ability]"
/// </summary>
[OracleEffect("loseAbility")]
public sealed record LoseAbilityEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The ability text that is lost, or "all abilities"
  /// </summary>
  [JsonPropertyName("abilityText")]
  public required string AbilityText { get; init; }
}
