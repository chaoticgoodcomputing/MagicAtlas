namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[target] gains [ability]"
/// </summary>
[OracleEffect("gainAbility")]
public sealed record GainAbilityEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The ability that is gained, as a structured AST node.
  /// Recursive: a granted ability is itself an <see cref="Ability"/> with its
  /// own costs, effects, restrictions, etc. (Rule 113.6 / 113.10 — abilities
  /// granted by an effect are still full-fledged abilities of the gainer.)
  /// </summary>
  public required Ability GainedAbility { get; init; }
}
