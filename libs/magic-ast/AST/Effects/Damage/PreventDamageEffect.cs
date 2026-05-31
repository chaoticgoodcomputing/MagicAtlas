namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "prevent [damage]"
/// </summary>
[OracleEffect("preventDamage")]
public sealed record PreventDamageEffect : ContinuousEffect
{
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

  /// <summary>
  /// True if preventing all damage (no quantity limit).
  /// </summary>
  public bool All { get; init; }

  /// <summary>
  /// True if only combat damage is prevented (oracle says "combat damage").
  /// False/absent means all damage (regardless of source type) is prevented.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool CombatOnly { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Source { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
