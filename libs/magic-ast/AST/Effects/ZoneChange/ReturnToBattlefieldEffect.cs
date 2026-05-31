namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "return [target] to the battlefield"
/// </summary>
[OracleEffect("returnToBattlefield")]
public sealed record ReturnToBattlefieldEffect : Effect
{
  public required ObjectReference Target { get; init; }

  public bool Tapped { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? UnderControl { get; init; }

  /// <summary>
  /// "return it to the battlefield transformed" — the permanent re-enters
  /// transformed, i.e. with its back face up (CR 712; The Legend of Roku final
  /// chapter, a transforming Saga). Null/false when it returns with its front face up.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? Transformed { get; init; }

  /// <summary>
  /// "return it to the battlefield with [N] [type] counters on it" — counters placed
  /// on the permanent as part of the return action (Persist/Undying-style patterns).
  /// Parallel to <see cref="ExileEffect.WithCounters"/>: the return gains this
  /// structure only when the card prints the counter modifier.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public CounterPlacement? WithCounters { get; init; }
}
