namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for hand-parsed card ASTs.
/// Each test case is loaded from a JSON file in the Data/HandParsedCards directory.
/// </summary>
[TestFixture]
public class HandParsedCardTests
{
  /// <summary>
  /// JSON options used for serialization during tests.
  /// </summary>
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  /// <summary>
  /// Test 1: Round-trip serialization.
  /// Deserializing the output and re-serializing it should produce semantically identical JSON.
  /// This validates that our type definitions correctly model the expected output format.
  /// HARD ASSERTION: Failure means the output format is unrepresentable by our types.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
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
  /// Test 2: Parser produces expected output.
  /// Parsing the input DTO should produce an AST that matches the expected output from the JSON file.
  /// RATCHET ASSERTION: Compared against baseline to detect regressions only.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Parser_ProducesExpectedOutput(CardTestCase testCase)
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
    var testName = $"HandParsedCards/{testCase.Name}";
    RatchetTestTracker.Instance.RecordResult(testName, passed);

    // Track failure patterns for hotspot analysis
    if (!passed)
    {
      var pattern = ExtractFailurePattern(result.Output);
      FailureTracker.RecordFailure(testCase.Name, pattern);
    }

    // Assert - parser output must match expected output (minimal error message)
    Assert.That(
      passed,
      Is.True,
      $"Parser mismatch: {testCase.Name} (see failure summary for patterns)"
    );
  }

  /// <summary>
  /// Extracts the primary failure pattern from a parsed card for categorization.
  /// </summary>
  private static string ExtractFailurePattern(CardOutputAST card)
  {
    // Check for unparsed abilities and extract their diagnostic patterns
    var unparsedAbilities = card
      .Oracle?.Abilities?.OfType<MagicAST.AST.Abilities.UnparsedAbility>()
      .ToList();

    if (unparsedAbilities?.Any() == true)
    {
      // Get the most common pattern from diagnostics
      var patterns = unparsedAbilities
        .SelectMany(a => a.Diagnostics ?? [])
        .Select(d => d.Pattern ?? "Unknown")
        .GroupBy(p => p)
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault();

      return patterns ?? "UnparsedAbility";
    }

    // Check for field-level mismatches (e.g., extra isOptional fields)
    return "FieldMismatch";
  }
}
