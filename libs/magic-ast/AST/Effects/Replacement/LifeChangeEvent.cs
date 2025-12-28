namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Life change event: "would gain/lose life"
/// </summary>
public sealed record LifeChangeEvent : ReplacementEvent
{
  /// <summary>
  /// Whether this is life gain or life loss.
  /// </summary>
  [JsonPropertyName("changeType")]
  public required string ChangeType { get; init; }
}
