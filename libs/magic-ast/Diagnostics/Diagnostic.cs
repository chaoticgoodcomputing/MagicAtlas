namespace MagicAST.Diagnostics;

using System.Text.Json.Serialization;
using MagicAST.AST;

/// <summary>
/// Severity level for diagnostics.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DiagnosticSeverity>))]
public enum DiagnosticSeverity
{
  /// <summary>Informational message, parsing succeeded.</summary>
  [JsonStringEnumMemberName("info")]
  Info,

  /// <summary>Warning, parsing succeeded but result may be incomplete.</summary>
  [JsonStringEnumMemberName("warning")]
  Warning,

  /// <summary>Error, parsing failed for this section.</summary>
  [JsonStringEnumMemberName("error")]
  Error,
}

/// <summary>
/// Represents a diagnostic message from parsing.
/// </summary>
public sealed record Diagnostic
{
  /// <summary>
  /// The severity of this diagnostic.
  /// </summary>
  [JsonPropertyName("severity")]
  public required DiagnosticSeverity Severity { get; init; }

  /// <summary>
  /// Human-readable description of the issue.
  /// </summary>
  [JsonPropertyName("message")]
  public required string Message { get; init; }

  /// <summary>
  /// Location in the source text where the issue occurred.
  /// </summary>
  [JsonPropertyName("location")]
  public required TextSpan Location { get; init; }

  /// <summary>
  /// What the parser expected to find.
  /// </summary>
  [JsonPropertyName("expected")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Expected { get; init; }

  /// <summary>
  /// What was actually found.
  /// </summary>
  [JsonPropertyName("actual")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Actual { get; init; }

  /// <summary>
  /// The raw text fragment that caused the issue.
  /// </summary>
  [JsonPropertyName("rawText")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RawText { get; init; }

  /// <summary>
  /// Categorized failure pattern for aggregation.
  /// e.g., "UnknownKeyword", "MalformedCost", "NestedAbility"
  /// </summary>
  [JsonPropertyName("pattern")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Pattern { get; init; }
}
