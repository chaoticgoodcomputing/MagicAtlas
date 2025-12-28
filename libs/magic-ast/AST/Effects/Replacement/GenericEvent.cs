namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Generic/unparsed event for complex cases.
/// </summary>
public sealed record GenericEvent : ReplacementEvent
{
  /// <summary>
  /// Raw text description of the event.
  /// </summary>
  [JsonPropertyName("description")]
  public required string Description { get; init; }
}
