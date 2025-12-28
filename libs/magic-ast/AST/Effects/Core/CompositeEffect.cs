namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Multiple effects combined.
/// </summary>
public sealed record CompositeEffect : Effect
{
  [JsonPropertyName("effects")]
  public required IReadOnlyList<Effect> Effects { get; init; }
}
