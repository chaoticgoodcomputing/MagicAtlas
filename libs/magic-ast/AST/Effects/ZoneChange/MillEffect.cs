namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "mill [count] cards"
/// </summary>
[OracleEffect("mill")]
public sealed record MillEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
