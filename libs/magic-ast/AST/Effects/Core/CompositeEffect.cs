namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Multiple effects combined.
/// </summary>
[OracleEffect("composite")]
public sealed record CompositeEffect : Effect
{
  [JsonPropertyName("effects")]
  public required IReadOnlyList<Effect> Effects { get; init; }
}
