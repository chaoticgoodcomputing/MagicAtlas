namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "tap [target]"
/// </summary>
[OracleEffect("tap")]
public sealed record TapEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }
}
