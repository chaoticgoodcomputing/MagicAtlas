namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "gain [amount] life"
/// </summary>
[OracleEffect("gainLife")]
public sealed record GainLifeEffect : Effect
{
  public required Quantity Amount { get; init; }

  public required ObjectReference Player { get; init; }

  /// <summary>
  /// Game-state condition that must hold for this life gain to occur.
  /// Used for "If [condition], you gain N life" patterns such as
  /// "If this spell was kicked, you gain life equal to the life lost this way."
  /// Mirrors the <see cref="Condition"/> used by <see cref="DrawCardsEffect.Condition"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
