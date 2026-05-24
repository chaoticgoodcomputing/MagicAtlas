namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "scry [count]"
/// </summary>
[OracleEffect("scry")]
public sealed record ScryEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }
}
