namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "mill [count] cards"
/// </summary>
[OracleEffect("mill")]
public sealed record MillEffect : Effect
{
  public required Quantity Count { get; init; }

  public required ObjectReference Player { get; init; }

  /// <summary>
  /// Optional game-state condition that must hold for this mill to occur.
  /// Used for "If this spell was kicked, instead mill half their library" patterns —
  /// a condition-gated replacement variant of the base mill. Mirrors the
  /// <see cref="DrawCardsEffect.Condition"/> field (CR 702.33d kicker state;
  /// CR 702.33e "if this spell was kicked" resolution).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
