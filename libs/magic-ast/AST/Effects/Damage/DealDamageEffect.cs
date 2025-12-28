namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "deals N damage to [target]"
/// </summary>
public sealed record DealDamageEffect : Effect
{
  [JsonPropertyName("amount")]
  public required Quantity Amount { get; init; }

  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  [JsonPropertyName("source")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Source { get; init; }
}
