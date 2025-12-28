namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "lose [amount] life"
/// </summary>
public sealed record LoseLifeEffect : Effect
{
  [JsonPropertyName("amount")]
  public required Quantity Amount { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
