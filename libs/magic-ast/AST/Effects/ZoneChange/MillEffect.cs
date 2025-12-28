namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "mill [count] cards"
/// </summary>
public sealed record MillEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
