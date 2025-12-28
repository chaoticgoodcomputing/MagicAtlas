namespace MagicAST;

using System.Text.Json.Serialization;
using MagicAST.AST;
using MagicAST.Diagnostics;

/// <summary>
/// The result of parsing a card's oracle text.
/// </summary>
public sealed record ParseResult
{
  /// <summary>
  /// The parsed AST, if parsing succeeded at all.
  /// </summary>
  [JsonPropertyName("output")]
  public required CardOracle Output { get; init; }

  /// <summary>
  /// The overall parse status.
  /// </summary>
  [JsonPropertyName("status")]
  public required ParseStatus Status { get; init; }

  /// <summary>
  /// Diagnostics from parsing (errors, warnings, info).
  /// </summary>
  [JsonPropertyName("diagnostics")]
  public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// Metrics about the parse operation.
  /// </summary>
  [JsonPropertyName("metrics")]
  public required ParseMetrics Metrics { get; init; }
}

/// <summary>
/// Overall status of a parse operation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParseStatus>))]
public enum ParseStatus
{
  /// <summary>All abilities were fully parsed.</summary>
  [JsonStringEnumMemberName("fullyParsed")]
  FullyParsed,

  /// <summary>Some abilities were parsed, some failed.</summary>
  [JsonStringEnumMemberName("partial")]
  Partial,

  /// <summary>No abilities could be parsed.</summary>
  [JsonStringEnumMemberName("failed")]
  Failed,
}

/// <summary>
/// Metrics about a parse operation.
/// </summary>
public sealed record ParseMetrics
{
  /// <summary>
  /// Total number of abilities found.
  /// </summary>
  [JsonPropertyName("totalAbilities")]
  public required int TotalAbilities { get; init; }

  /// <summary>
  /// Number of abilities fully parsed.
  /// </summary>
  [JsonPropertyName("parsedAbilities")]
  public required int ParsedAbilities { get; init; }

  /// <summary>
  /// Number of abilities that failed to parse.
  /// </summary>
  [JsonPropertyName("failedAbilities")]
  public required int FailedAbilities { get; init; }

  /// <summary>
  /// Parse duration in milliseconds.
  /// </summary>
  [JsonPropertyName("durationMs")]
  public required double DurationMs { get; init; }

  /// <summary>
  /// Percentage of abilities successfully parsed.
  /// </summary>
  [JsonPropertyName("successRate")]
  public double SuccessRate => TotalAbilities == 0 ? 0 : (double)ParsedAbilities / TotalAbilities;
}
