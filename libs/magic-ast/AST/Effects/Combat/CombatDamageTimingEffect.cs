namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Combat damage timing effect: modifies when this creature deals combat damage.
/// Covers: First Strike, Double Strike
/// </summary>
public sealed record CombatDamageTimingEffect : Effect
{
  /// <summary>
  /// When this creature deals combat damage.
  /// - "first": First strike (before normal combat damage)
  /// - "both": Double strike (first strike AND normal combat damage)
  /// </summary>
  [JsonPropertyName("timing")]
  public required CombatDamageTiming Timing { get; init; }
}
