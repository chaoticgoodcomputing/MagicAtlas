namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "gain [amount] life"
/// </summary>
[OracleEffect("gainLife")]
public sealed record GainLifeEffect : Effect
{
  [JsonPropertyName("amount")]
  public required Quantity Amount { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
