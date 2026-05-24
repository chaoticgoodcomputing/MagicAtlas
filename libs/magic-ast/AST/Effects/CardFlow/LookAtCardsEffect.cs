namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Look at the top N cards of your library" or "Look at target player's hand"
/// </summary>
[OracleEffect("lookAtCards")]
public sealed record LookAtCardsEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
