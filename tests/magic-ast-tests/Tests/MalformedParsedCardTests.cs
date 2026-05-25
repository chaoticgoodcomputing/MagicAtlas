namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for malformed/unparsed card ASTs. These validate that UnparsedAbility,
/// UnparsedEffect, and related types correctly round-trip and that the parser
/// gracefully produces structured "unparsed" output rather than throwing.
/// </summary>
[TestFixture]
public class MalformedParsedCardTests
{
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  [TestCaseSource(
    typeof(MalformedParsedTestCaseLoader),
    nameof(MalformedParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Output_RoundTrip_ProducesIdenticalJson(CardTestCase testCase)
  {
    var expectedNode = testCase.OutputNode;

    var ast = testCase.GetOutput();
    var actualJson = JsonSerializer.Serialize(ast, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, expectedNode);

    Assert.That(
      passed,
      Is.True,
      $"Round-trip serialization failed for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(expectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }

  [TestCaseSource(
    typeof(MalformedParsedTestCaseLoader),
    nameof(MalformedParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Parser_ProducesExpectedUnparsedOutput(CardTestCase testCase)
  {
    var input = testCase.GetInput();
    var parser = new CardParser();
    var expectedNode = testCase.OutputNode;

    var result = parser.Parse(input);
    var actualJson = JsonSerializer.Serialize(result.Output, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, expectedNode);

    Assert.That(passed, Is.True, $"Malformed card mismatch: {testCase.Name}");
  }
}
