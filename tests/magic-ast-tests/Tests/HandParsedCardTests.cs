namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Tests for hand-parsed card ASTs.
/// Each test case is loaded from a JSON file in the Data/HandParsedCards directory.
/// Both tests must pass green for every fixture — no ratchet tolerance.
/// </summary>
[TestFixture]
public class HandParsedCardTests
{
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  /// <summary>
  /// Schema integrity: deserializing the expected output and re-serializing it
  /// must produce semantically identical JSON. Failure means the gold AST is
  /// unrepresentable by our type system.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
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

  /// <summary>
  /// Parser correctness: parsing the input DTO must produce an AST matching
  /// the gold output. Both tests gate batch landing — no batch lands red.
  /// </summary>
  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Parser_ProducesExpectedOutput(CardTestCase testCase)
  {
    var input = testCase.GetInput();
    var parser = new CardParser();
    var expectedNode = testCase.OutputNode;

    var result = parser.Parse(input);
    var actualJson = JsonSerializer.Serialize(result.Output, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, expectedNode);

    if (!passed)
    {
      try
      {
        var diffDir = "/tmp/mast-diffs";
        Directory.CreateDirectory(diffDir);
        var safeName = testCase.Name.Replace('/', '_');
        var expJson = expectedNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "(null)";
        var actJson = actualNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "(null)";
        File.WriteAllText(Path.Combine(diffDir, $"{safeName}.expected.json"), expJson);
        File.WriteAllText(Path.Combine(diffDir, $"{safeName}.actual.json"), actJson);
      }
      catch { /* best-effort dump */ }
    }

    Assert.That(
      passed,
      Is.True,
      $"Parser mismatch for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(expectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }
}
