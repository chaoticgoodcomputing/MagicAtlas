namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for malformed/unparsed card ASTs.
/// Each test case is loaded from a JSON file in the Data/MalformedParsedCards directory.
/// These tests validate that the Unparsed AST node types serialize/deserialize correctly.
/// </summary>
[TestFixture]
public class MalformedParsedCardTests
{
  /// <summary>
  /// JSON options used for serialization during tests.
  /// </summary>
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  /// <summary>
  /// Test 1: Round-trip serialization for malformed cards.
  /// Deserializing the output and re-serializing it should produce semantically identical JSON.
  /// This validates that UnparsedAbility, UnparsedEffect, and related types correctly round-trip.
  /// HARD ASSERTION: Failure means the output format is unrepresentable by our types.
  /// </summary>
  [TestCaseSource(
    typeof(MalformedParsedTestCaseLoader),
    nameof(MalformedParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Output_RoundTrip_ProducesIdenticalJson(CardTestCase testCase)
  {
    // Arrange
    var expectedNode = testCase.OutputNode;

    // Act
    var ast = testCase.GetOutput();
    var actualJson = JsonSerializer.Serialize(ast, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);

    // Assert - compare JSON structures, not string representations
    Assert.That(
      JsonComparer.AreEqual(actualNode, expectedNode),
      Is.True,
      $"Round-trip serialization failed for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(expectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }

  /// <summary>
  /// Test 2: Parser produces expected unparsed output.
  /// Parsing the input DTO (with malformed oracle text) should produce an AST
  /// containing UnparsedAbility/UnparsedEffect nodes that serialize to the expected output.
  /// RATCHET ASSERTION: Compared against baseline to detect regressions only.
  /// </summary>
  [TestCaseSource(
    typeof(MalformedParsedTestCaseLoader),
    nameof(MalformedParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Parser_ProducesExpectedUnparsedOutput(CardTestCase testCase)
  {
    // Arrange
    var input = testCase.GetInput();
    var parser = new CardParser();
    var expectedNode = testCase.OutputNode;

    // Act
    var result = parser.Parse(input);
    var actualJson = JsonSerializer.Serialize(result.Output, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, expectedNode);

    // Record result in ratchet tracker
    var testName = $"MalformedParsedCards/{testCase.Name}";
    RatchetTestTracker.Instance.RecordResult(testName, passed);

    // Track failure patterns
    if (!passed)
    {
      FailureTracker.RecordFailure(testCase.Name, "MalformedCard");
    }

    // Assert - parser output must match expected output (minimal message)
    Assert.That(passed, Is.True, $"Malformed card mismatch: {testCase.Name}");
  }
}
