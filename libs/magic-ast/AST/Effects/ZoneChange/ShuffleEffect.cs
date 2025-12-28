namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "shuffle"
/// </summary>
public sealed record ShuffleEffect : Effect
{
  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
