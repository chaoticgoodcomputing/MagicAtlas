namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "shuffle"
/// </summary>
[OracleEffect("shuffle")]
public sealed record ShuffleEffect : Effect
{
  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }
}
