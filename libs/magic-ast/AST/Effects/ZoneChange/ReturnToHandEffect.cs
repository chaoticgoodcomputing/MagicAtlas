namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "return [target] to its owner's hand"
/// </summary>
[OracleEffect("returnToHand")]
public sealed record ReturnToHandEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }
}
