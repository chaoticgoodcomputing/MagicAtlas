namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Damage event: "damage would be dealt"
/// </summary>
[OracleReplacementEvent("damage")]
public sealed record DamageEvent : ReplacementEvent
{
  /// <summary>
  /// Source of the damage (null = any source).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Source { get; init; }

  /// <summary>
  /// Whether this is specifically combat or noncombat damage.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? DamageType { get; init; }
}
