namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Protection effect: comprehensive immunity from a quality.
/// "This permanent can't be blocked, targeted, dealt damage, enchanted, or equipped by [quality]."
/// Rule 702.16
/// </summary>
public sealed record ProtectionEffect : Effect
{
  /// <summary>
  /// The qualities this permanent has protection from.
  /// Can be colors ("red"), card types ("Demons"), or other qualities ("everything").
  /// </summary>
  [JsonPropertyName("from")]
  public required IReadOnlyList<ProtectionQuality> From { get; init; }
}
