namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "scry [count]"
/// </summary>
public sealed record ScryEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }
}
