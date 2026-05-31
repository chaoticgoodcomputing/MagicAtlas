namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "draw [count] cards"
/// </summary>
[OracleEffect("drawCards")]
public sealed record DrawCardsEffect : Effect
{
  public required Quantity Count { get; init; }

  public required ObjectReference Player { get; init; }

  /// <summary>
  /// Game-state condition that must hold for this draw to occur.
  /// Used for "If [condition], draw a card" patterns such as "If this spell was kicked".
  /// Mirrors the <see cref="Condition"/> used by <see cref="TriggeredAbility.InterveningIf"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
