namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "discard [count] cards"
/// </summary>
[OracleEffect("discardCards")]
public sealed record DiscardCardsEffect : Effect
{
  public required Quantity Count { get; init; }

  public required ObjectReference Player { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// True if the discard is random.
  /// </summary>
  public bool Random { get; init; }
}
