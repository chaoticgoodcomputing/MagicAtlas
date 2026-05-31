namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "exile [target]"
/// </summary>
[OracleEffect("exile")]
public sealed record ExileEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "until [condition]" for temporary exile
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ReturnCondition { get; init; }

  /// <summary>
  /// "exile [target] with [N] [type] counters on it" — counters placed on
  /// the card as part of the exile action (suspend-like patterns).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public CounterPlacement? WithCounters { get; init; }
}
