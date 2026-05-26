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
  public required DiagnosticSeverity Severity { get; init; }

  /// <summary>
  /// Human-readable description of the issue.
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// Location in the source text where the issue occurred.
  /// </summary>
  public required TextSpan Location { get; init; }

  /// <summary>
  /// What the parser expected to find.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Expected { get; init; }

  /// <summary>
  /// What was actually found.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Actual { get; init; }

  /// <summary>
  /// The raw text fragment that caused the issue.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? RawText { get; init; }

  /// <summary>
  /// Categorized failure pattern for aggregation.
  /// e.g., "UnknownKeyword", "MalformedCost", "NestedAbility"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Pattern { get; init; }

  /// <summary>
  /// Name of the parser rule that came closest to matching this clause before
  /// the dispatch gave up. Format <c>"{ParserClassName}.{MethodName}"</c>
  /// (e.g. <c>"SpellAbilityParser.Parse"</c>). Combined with
  /// <see cref="Pattern"/> in the triage aggregator to produce
  /// finer-grained failure clusters (so e.g. a "ConditionalEffect" pattern
  /// arriving via the spell dispatch chain is distinguished from the same
  /// pattern arriving via the triggered dispatch chain).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? LastAttemptedRule { get; init; }

  /// <summary>
  /// Character offset within the source oracle text at which the parser bailed
  /// out. For clause-level fall-throughs this is the clause's start offset
  /// (i.e. <see cref="MagicAST.AST.TextSpan.Start"/> of the clause being
  /// dispatched), which is the most precise position the current
  /// regex-rule-chain architecture surfaces without per-sub-rule
  /// instrumentation.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? FailurePosition { get; init; }
}
