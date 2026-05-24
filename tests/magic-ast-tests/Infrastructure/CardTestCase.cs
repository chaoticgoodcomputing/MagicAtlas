namespace MagicAST.Tests.Infrastructure;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Base class representing a test case loaded from a JSON file.
/// Contains input (CardInputDTO) and expected output (CardOutputAST).
/// </summary>
public sealed class CardTestCase
{
  /// <summary>
  /// The name of the test case (derived from filename).
  /// </summary>
  public required string Name { get; init; }

  /// <summary>
  /// Path to the source JSON file.
  /// </summary>
  public required string FilePath { get; init; }

  /// <summary>
  /// The raw "input" JSON node from the file.
  /// </summary>
  public required JsonNode InputNode { get; init; }

  /// <summary>
  /// The raw "output" JSON node from the file.
  /// </summary>
  public required JsonNode OutputNode { get; init; }

  /// <summary>
  /// Deserializes the input node to a CardInputDTO.
  /// </summary>
  public CardInputDTO GetInput() =>
    InputNode.Deserialize<CardInputDTO>(MagicASTJsonOptions.Strict)
    ?? throw new InvalidOperationException($"Failed to deserialize input from {FilePath}");

  /// <summary>
  /// Deserializes the output node to a CardOutputAST.
  /// </summary>
  public CardOutputAST GetOutput() =>
    OutputNode.Deserialize<CardOutputAST>(MagicASTJsonOptions.Strict)
    ?? throw new InvalidOperationException($"Failed to deserialize output from {FilePath}");

  /// <summary>
  /// Gets the output JSON as a normalized string for comparison.
  /// </summary>
  public string GetOutputJson() => OutputNode.ToJsonString(_serializationTestOptions);

  public override string ToString() => Name;

  /// <summary>
  /// Options for serialization during tests - consistent formatting for comparison.
  /// </summary>
  private static readonly JsonSerializerOptions _serializationTestOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };
}
