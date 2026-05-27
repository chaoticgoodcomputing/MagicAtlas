namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "you get {E}", "you get {E}{E}", etc. — the player gains N energy counters.
/// Rule 107.4f / 107.6: An energy counter is a counter that can be placed on a player.
/// Each {E} symbol in the cost-style notation represents one energy counter.
/// MAST-descriptive: this models the verb + count. Player-state bookkeeping
/// (where the counters live, how they're spent) is engine territory.
/// </summary>
[OracleEffect("gainEnergy")]
public sealed record GainEnergyEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
