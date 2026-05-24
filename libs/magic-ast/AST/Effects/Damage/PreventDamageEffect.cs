namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "prevent [damage]"
/// </summary>
[OracleEffect("preventDamage")]
public sealed record PreventDamageEffect : Effect
{
  [JsonPropertyName("amount")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

  /// <summary>
  /// True if preventing all damage.
  /// </summary>
  [JsonPropertyName("all")]
  public bool All { get; init; }

  [JsonPropertyName("source")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Source { get; init; }

  [JsonPropertyName("target")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
