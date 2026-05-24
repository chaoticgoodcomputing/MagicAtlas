namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "gain control of [target]"
/// </summary>
[OracleEffect("gainControl")]
public sealed record GainControlEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }
}
