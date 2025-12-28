namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "discard [count] cards"
/// </summary>
public sealed record DiscardCardsEffect : Effect
{
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }

  [JsonPropertyName("filter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// True if the discard is random.
  /// </summary>
  [JsonPropertyName("random")]
  public bool Random { get; init; }
}
