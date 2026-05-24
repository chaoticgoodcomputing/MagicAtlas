namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You get an emblem with [abilities]"
/// Rule 114
/// </summary>
[OracleEffect("createEmblem")]
public sealed record CreateEmblemEffect : Effect
{
  /// <summary>
  /// The emblem definition with its abilities.
  /// </summary>
  [JsonPropertyName("emblem")]
  public required EmblemDefinition Emblem { get; init; }
}
