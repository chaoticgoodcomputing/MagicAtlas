namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "you get {E}", "you get {E}{E}", etc. — the player gains N energy counters.
/// Rule 107.14: Energy counters are a player resource represented by the {E} symbol;
/// see rule 122 for the generic counter framework.
/// Each {E} symbol in the cost-style notation represents one energy counter.
/// MAST-descriptive: this models the verb + count. Player-state bookkeeping
/// (where the counters live, how they're spent) is engine territory.
/// </summary>
[OracleEffect("gainEnergy")]
public sealed record GainEnergyEffect : Effect
{
  /// <summary>How many {E} (energy counters) the player receives.</summary>
  public required Quantity Amount { get; init; }

  /// <summary>
  /// Which player gains the energy. Defaults to "you" (the controller of the ability).
  /// Modeled the same way as <see cref="GainLifeEffect.Player"/> — required, with
  /// <see cref="ObjectReference.You"/> being the implicit subject in nearly all
  /// printed ETB-energy oracle lines.
  /// </summary>
  public required ObjectReference Player { get; init; }
}
