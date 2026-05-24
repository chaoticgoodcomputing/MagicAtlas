namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "return [target] to the battlefield"
/// </summary>
[OracleEffect("returnToBattlefield")]
public sealed record ReturnToBattlefieldEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  [JsonPropertyName("tapped")]
  public bool Tapped { get; init; }

  [JsonPropertyName("underControl")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? UnderControl { get; init; }
}
