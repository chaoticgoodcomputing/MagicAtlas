namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "gain control of [target]"
/// </summary>
public sealed record GainControlEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }
}
