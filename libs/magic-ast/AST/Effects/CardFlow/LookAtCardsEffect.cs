namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Look at the top N cards of your library" or "Look at target player's hand"
/// </summary>
public sealed record LookAtCardsEffect : Effect
{
  /// <summary>
  /// Whose cards to look at.
  /// </summary>
  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards to look at.
  /// </summary>
  [JsonPropertyName("count")]
  public required Quantity Count { get; init; }

  /// <summary>
  /// Where to look: Library, Hand, etc.
  /// </summary>
  [JsonPropertyName("zone")]
  public required Zone Zone { get; init; }

  /// <summary>
  /// Where in the zone: Top, Bottom, Random, All.
  /// </summary>
  [JsonPropertyName("location")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Location { get; init; }
}
