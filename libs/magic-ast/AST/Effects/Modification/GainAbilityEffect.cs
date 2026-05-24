namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[target] gains [ability]"
/// </summary>
[OracleEffect("gainAbility")]
public sealed record GainAbilityEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The ability text that is gained.
  /// </summary>
  [JsonPropertyName("abilityText")]
  public required string AbilityText { get; init; }

  // TODO: Could expand to full Ability node if needed
}
