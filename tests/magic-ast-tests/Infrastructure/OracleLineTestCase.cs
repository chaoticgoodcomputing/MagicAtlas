namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;

/// <summary>
/// Represents a single line of oracle text to be tested.
/// Granular unit test case for individual ability parsing.
/// </summary>
public sealed class OracleLineTestCase
{
  /// <summary>
  /// Unique identifier for this test case.
  /// Format: "CardName/Line{N}" where N is the line number (1-indexed).
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// The raw oracle text line to parse.
  /// </summary>
  public required string OracleText { get; init; }

  /// <summary>
  /// The expected parsed ability (JSON node from output.oracle.abilities[N]).
  /// </summary>
  public required JsonNode ExpectedAbility { get; init; }

  /// <summary>
  /// The card this oracle line came from (for context/debugging).
  /// </summary>
  public required string SourceCard { get; init; }

  /// <summary>
  /// The line number within the card's oracle text (1-indexed).
  /// </summary>
  public required int LineNumber { get; init; }

  /// <summary>
  /// The original file path this test case was extracted from.
  /// </summary>
  public required string SourceFilePath { get; init; }
}
