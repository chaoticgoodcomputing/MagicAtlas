namespace MagicAST.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;
using NUnit.Framework;

/// <summary>
/// Tests oracle text parsing at a granular level - one test per oracle text line.
/// </summary>
/// <remarks>
/// This test class enables family-based parser development by testing individual
/// oracle text lines rather than complete cards. This makes it easier to:
/// - Identify which specific ability patterns are failing
/// - Group failures by ability family (triggered abilities, activated abilities, etc.)
/// - Develop parsers incrementally by focusing on high-value patterns
/// - See parser improvements at a more granular level than full card tests
/// </remarks>
[TestFixture]
public class OracleLineTests
{
  /// <summary>
  /// Parses a single line of oracle text and verifies it matches the expected ability.
  /// </summary>
  [TestCaseSource(
    typeof(OracleLineTestCaseLoader),
    nameof(OracleLineTestCaseLoader.GetTestCaseData)
  )]
  public void ParseOracleLine_ShouldMatchExpectedAbility(OracleLineTestCase testCase)
  {
    // Arrange
    var parser = new OracleParser();

    // Act
    var result = parser.Parse(testCase.OracleText);

    // For oracle lines with comma-separated keywords, we expect multiple abilities
    // For other lines, we expect exactly one ability
    List<JsonNode?> expectedAbilities;
    if (testCase.ExpectedAbility is JsonArray array)
    {
      expectedAbilities = array.ToList();
    }
    else
    {
      expectedAbilities = new List<JsonNode?> { testCase.ExpectedAbility };
    }

    // Use Assert.Multiple to report ALL failures, not just the first one
    Assert.Multiple(() =>
    {
      // Check 1: Parsing errors
      var errors = result.Diagnostics.Where(d =>
        d.Severity == MagicAST.Diagnostics.DiagnosticSeverity.Error
      );
      Assert.That(
        errors,
        Is.Empty,
        $"[DIAGNOSTICS] Oracle line parsing had errors: {testCase.OracleText}\n"
          + string.Join("\n", errors.Select(e => $"  - {e.Message}"))
      );

      // Check 2: Ability count
      Assert.That(
        result.Output.Abilities,
        Has.Count.EqualTo(expectedAbilities.Count),
        $"[ABILITY COUNT] Expected {expectedAbilities.Count} ability/abilities from line: {testCase.OracleText}"
      );

      // Check 3: Each ability structure matches
      var abilityCount = Math.Min(result.Output.Abilities.Count, expectedAbilities.Count);
      for (int i = 0; i < abilityCount; i++)
      {
        var parsedAbility = result.Output.Abilities[i];
        var parsedJson = JsonSerializer.SerializeToNode(
          parsedAbility,
          new JsonSerializerOptions { WriteIndented = false }
        );

        var expectedJson = expectedAbilities[i];

        // Use semantic JSON comparison that ignores property order and default values
        var isEqual = JsonComparer.AreEqual(expectedJson, parsedJson);

        Assert.That(
          isEqual,
          Is.True,
          $"[ABILITY {i + 1} STRUCTURE] Parsed ability does not match expected for line {testCase.LineNumber} of {testCase.SourceCard}:\n"
            + $"Oracle text: {testCase.OracleText}\n\n"
            + $"Expected:\n{JsonComparer.FormatForDisplay(expectedJson)}\n\n"
            + $"Actual:\n{JsonComparer.FormatForDisplay(parsedJson)}"
        );
      }
    });
  }
}
