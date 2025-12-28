using Flowthru.Abstractions;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Report of parsing diagnostics (errors or warnings) aggregated across all cards.
/// </summary>
/// <remarks>
/// Provides insight into the most common parsing issues in the MagicAST library.
/// Useful for prioritizing which oracle text patterns to implement next.
/// </remarks>
public record DiagnosticReport : ITextSerializable, IStructuredSerializable, INestedSerializable
{
  /// <summary>
  /// Diagnostic code (e.g., "AST001", "AST999").
  /// </summary>
  public required string Code { get; init; }

  /// <summary>
  /// Diagnostic message template.
  /// </summary>
  public required string Message { get; init; }

  /// <summary>
  /// Total number of occurrences of this diagnostic across all cards.
  /// </summary>
  public required int Count { get; init; }

  /// <summary>
  /// Example cards that triggered this diagnostic.
  /// Limited to a reasonable number for review.
  /// </summary>
  public required List<DiagnosticExample> Examples { get; init; }

  /// <summary>
  /// Total number of cards analyzed.
  /// </summary>
  public required int TotalCards { get; init; }

  /// <summary>
  /// Percentage of cards successfully parsed without this diagnostic (0-100).
  /// For error reports: percentage without errors.
  /// For warning reports: percentage without warnings.
  /// </summary>
  public required double PercentageSuccessful { get; init; }
}

/// <summary>
/// Example of a card that triggered a specific diagnostic.
/// </summary>
public record DiagnosticExample
{
  /// <summary>
  /// Card name that triggered the diagnostic.
  /// </summary>
  public required string CardName { get; init; }

  /// <summary>
  /// The source text that caused the diagnostic (e.g., oracle text snippet).
  /// </summary>
  public string? SourceText { get; init; }
}
