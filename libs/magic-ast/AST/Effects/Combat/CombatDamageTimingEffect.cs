namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Combat damage timing effect: modifies when this creature deals combat damage.
/// Covers: First Strike, Double Strike
/// </summary>
[OracleEffect("combatDamageTiming")]
public sealed record CombatDamageTimingEffect : Effect
{
  /// <summary>
  /// When this creature deals combat damage.
  /// - "first": First strike (before normal combat damage)
  /// - "both": Double strike (first strike AND normal combat damage)
  /// </summary>
  public required CombatDamageTiming Timing { get; init; }
}
