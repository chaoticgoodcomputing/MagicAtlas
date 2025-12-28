using Flowthru.Abstractions;
using MagicAST;
using MagicAST.Diagnostics;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// Output DTO representing a parsed Magic: The Gathering card AST with diagnostics.
/// Designed for pipeline output to JSON storage or downstream processing.
/// </summary>
/// <remarks>
/// This wraps the CardNode AST with serialization metadata and parse diagnostics.
/// Suitable for JSON output, database storage, or passing to downstream pipeline stages.
/// </remarks>
public record MagicAstCardAnalysis : ITextSerializable, IStructuredSerializable, INestedSerializable
{
  /// <summary>
  /// Card name from the input.
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// The parsed AST as a structured, serializable DTO.
  /// Null if parsing completely failed.
  /// </summary>
  public CardOutputAST? Ast { get; init; }

  /// <summary>
  /// Parse diagnostics (errors, warnings, info messages).
  /// </summary>
  public List<Diagnostic> Diagnostics { get; init; } = new();

  /// <summary>
  /// Whether parsing succeeded (AST is non-null and no errors).
  /// </summary>
  public bool ParseSucceeded { get; init; }
}
