namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Damage event: "damage would be dealt"
/// </summary>
public sealed record DamageEvent : ReplacementEvent
{
  /// <summary>
  /// Source of the damage (null = any source).
  /// </summary>
  [JsonPropertyName("source")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Source { get; init; }

  /// <summary>
  /// Whether this is specifically combat or noncombat damage.
  /// </summary>
  [JsonPropertyName("damageType")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DamageType { get; init; }
}
