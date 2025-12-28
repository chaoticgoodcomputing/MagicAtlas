namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Diagnostics;

/// <summary>
/// Represents an ability that could not be fully parsed.
/// Always contains the raw text and diagnostics explaining the failure.
/// </summary>
public sealed record UnparsedAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Unparsed;

  /// <summary>
  /// Location of this unparsed ability in the original oracle text.
  /// </summary>
  [JsonPropertyName("sourceSpan")]
  public required TextSpan SourceSpan { get; init; }

  /// <summary>
  /// The raw text that could not be parsed.
  /// </summary>
  [JsonPropertyName("rawText")]
  public required string RawText { get; init; }

  /// <summary>
  /// Diagnostics explaining why parsing failed.
  /// </summary>
  [JsonPropertyName("diagnostics")]
  public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// If parsing partially succeeded, the best-effort result.
  /// </summary>
  [JsonPropertyName("partialParse")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Ability? PartialParse { get; init; }
}
