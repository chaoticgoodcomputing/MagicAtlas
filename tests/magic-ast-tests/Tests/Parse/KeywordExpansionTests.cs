namespace MagicAST.Tests.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.Abilities;
using MagicAST.Keywords;
using MagicAST.Parsing.Tokens;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Keyword-expansion golds, "sans examples" (ADR 0003). Each gold pins the decomposed
/// <c>Ability</c> subtree a keyword's combinator must emit, fed only its canonical
/// printed form — no example card. Together the golds prove every keyword expands to a
/// CR-faithful shared-primitive subtree, and stand guard: the next silent
/// under-decomposition fails a test.
/// </summary>
[TestFixture]
public class KeywordExpansionTests
{
  private static readonly JsonSerializerOptions _testOptions =
    new(MagicASTJsonOptions.Strict) { WriteIndented = false };

  private static readonly OracleTokenizer _tokenizer = new();

  /// <summary>
  /// Schema integrity: the gold's expected subtree must deserialize to an
  /// <c>Ability</c> and re-serialize to semantically identical JSON. Failure means the
  /// gold AST is unrepresentable by our type system.
  /// </summary>
  [TestCaseSource(
    typeof(KeywordExpansionTestCaseLoader),
    nameof(KeywordExpansionTestCaseLoader.GetTestCaseData)
  )]
  public void Expected_RoundTrip_ProducesIdenticalJson(KeywordExpansionTestCase testCase)
  {
    var ability =
      testCase.ExpectedNode.Deserialize<Ability>(MagicASTJsonOptions.Strict)
      ?? throw new InvalidOperationException(
        $"Failed to deserialize Expected ability for {testCase.Name}"
      );

    var actualJson = JsonSerializer.Serialize(ability, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, testCase.ExpectedNode);

    Assert.That(
      passed,
      Is.True,
      $"Round-trip serialization failed for {testCase.Name}.\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(testCase.ExpectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }

  /// <summary>
  /// Expansion correctness: tokenizing the keyword's canonical printed form and running
  /// it through the production keyword combinator chain
  /// (<see cref="KeywordRegistry.RegisteredAnyKeyword"/>) must produce the gold's
  /// expected decomposed ability.
  /// </summary>
  [TestCaseSource(
    typeof(KeywordExpansionTestCaseLoader),
    nameof(KeywordExpansionTestCaseLoader.GetTestCaseData)
  )]
  public void Combinator_ProducesExpectedExpansion(KeywordExpansionTestCase testCase)
  {
    var tokenResult = _tokenizer.TryTokenize(testCase.PrintedForm);
    Assert.That(
      tokenResult.HasValue,
      Is.True,
      $"Tokenization failed for printed form '{testCase.PrintedForm}' ({testCase.Name})."
    );

    var parseResult = KeywordRegistry.RegisteredAnyKeyword(tokenResult.Value);
    Assert.That(
      parseResult.HasValue,
      Is.True,
      $"Keyword combinator failed to parse '{testCase.PrintedForm}' ({testCase.Name}): "
        + parseResult.ErrorMessage
    );

    var actualJson = JsonSerializer.Serialize(parseResult.Value, _testOptions);
    var actualNode = JsonNode.Parse(actualJson);
    var passed = JsonComparer.AreEqual(actualNode, testCase.ExpectedNode);

    if (!passed)
    {
      try
      {
        var diffDir = "/tmp/mast-diffs";
        System.IO.Directory.CreateDirectory(diffDir);
        var safeName = testCase.Name.Replace('/', '_');
        var expJson =
          testCase.ExpectedNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true })
          ?? "(null)";
        var actJson =
          actualNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "(null)";
        File.WriteAllText(Path.Combine(diffDir, $"kw_{safeName}.expected.json"), expJson);
        File.WriteAllText(Path.Combine(diffDir, $"kw_{safeName}.actual.json"), actJson);
      }
      catch
      {
        /* best-effort dump */
      }
    }

    Assert.That(
      passed,
      Is.True,
      $"Keyword expansion mismatch for {testCase.Name} (printed: '{testCase.PrintedForm}').\n"
        + $"Expected:\n{JsonComparer.FormatForDisplay(testCase.ExpectedNode)}\n\n"
        + $"Actual:\n{JsonComparer.FormatForDisplay(actualNode)}"
    );
  }
}
