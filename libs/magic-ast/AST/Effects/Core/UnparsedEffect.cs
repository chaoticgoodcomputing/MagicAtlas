namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// An effect that couldn't be parsed.
/// </summary>
public sealed record UnparsedEffect : Effect
{
  /// <summary>
  /// Location of this unparsed effect in the original oracle text.
  /// </summary>
  [JsonPropertyName("sourceSpan")]
  public required TextSpan SourceSpan { get; init; }

  [JsonPropertyName("rawText")]
  public required string RawText { get; init; }
}
