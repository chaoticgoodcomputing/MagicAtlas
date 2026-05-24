namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "draw [count] cards"
/// </summary>
[OracleEffect("drawCards")]
public sealed record DrawCardsEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
