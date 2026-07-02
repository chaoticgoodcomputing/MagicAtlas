namespace MagicAST;

using System.Linq;
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
  public required CardOracle Output { get; init; }

  /// <summary>
  /// The overall parse status.
  /// </summary>
  public required ParseStatus Status { get; init; }

  /// <summary>
  /// Diagnostics from parsing (errors, warnings, info).
  /// </summary>
  public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

  /// <summary>
  /// Metrics about the parse operation.
  /// </summary>
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
  public required int TotalAbilities { get; init; }

  /// <summary>
  /// Number of abilities fully parsed.
  /// </summary>
  public required int ParsedAbilities { get; init; }

  /// <summary>
  /// Number of abilities that failed to parse.
  /// </summary>
  public required int FailedAbilities { get; init; }

  /// <summary>
  /// Parse duration in milliseconds.
  /// </summary>
  public required double DurationMs { get; init; }

  /// <summary>
  /// Residual-debt tally for this parse (ADR 0001 forcing-function): free-text
  /// residuals reachable in the AST, keyed by kind — an <c>IResidual</c> node's
  /// type name, or <c>Type.Field</c> for a <c>[FreeTextField]</c>. Empty when the
  /// AST carries no residual debt. Distinct from <see cref="FailedAbilities"/>,
  /// which counts total ability-level parse failures; residuals are the
  /// not-yet-structured debt hiding inside otherwise-parsed ASTs.
  /// </summary>
  public required IReadOnlyDictionary<string, int> ResidualCounts { get; init; }

  /// <summary>
  /// Percentage of abilities successfully parsed.
  /// </summary>
  public double SuccessRate => TotalAbilities == 0 ? 0 : (double)ParsedAbilities / TotalAbilities;

  /// <summary>Total residual occurrences across all kinds.</summary>
  public int ResidualTotal => ResidualCounts.Values.Sum();
}
