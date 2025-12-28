namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Definition of an emblem created by a planeswalker.
/// </summary>
public sealed record EmblemDefinition
{
  /// <summary>
  /// The abilities the emblem has.
  /// </summary>
  [JsonPropertyName("abilities")]
  public required IReadOnlyList<MagicAST.AST.Abilities.Ability> Abilities { get; init; }
}
