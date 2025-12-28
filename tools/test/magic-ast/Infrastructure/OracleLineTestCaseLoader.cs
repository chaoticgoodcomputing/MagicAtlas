namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;
using MagicAST.Parsing;

/// <summary>
/// Loader that extracts individual oracle text lines from card test cases
/// and creates granular test cases for each line/ability.
/// </summary>
/// <remarks>
/// This allows for fine-grained testing of oracle text parsing, where each
/// line of oracle text becomes its own test case. This is particularly useful
/// for family-based parser development - you can see which specific ability
/// patterns are failing without testing entire cards.
/// </remarks>
public sealed class OracleLineTestCaseLoader
{
  private readonly CardTestCaseLoader _cardLoader;

  /// <summary>
  /// Creates a loader that extracts oracle lines from cards in the specified directory.
  /// </summary>
  /// <param name="dataSubdirectory">Subdirectory under Data/ (e.g., "HandParsedCards")</param>
  public OracleLineTestCaseLoader(string dataSubdirectory)
  {
    _cardLoader = new CardTestCaseLoader(dataSubdirectory);
  }

  /// <summary>
  /// Gets all oracle line test cases by extracting lines from card test cases.
  /// </summary>
  public IEnumerable<OracleLineTestCase> GetAllTestCases()
  {
    foreach (var cardTestCase in _cardLoader.GetAllTestCases())
    {
      foreach (var oracleLineTestCase in ExtractOracleLines(cardTestCase))
      {
        yield return oracleLineTestCase;
      }
    }
  }

  /// <summary>
  /// Extracts individual oracle line test cases from a card test case.
  /// </summary>
  private IEnumerable<OracleLineTestCase> ExtractOracleLines(CardTestCase cardTestCase)
  {
    // Get input oracle text
    var oracleText = cardTestCase.InputNode["oracleText"]?.GetValue<string>();
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      yield break;
    }

    // Get expected output abilities
    if (cardTestCase.OutputNode["oracle"] is not JsonObject outputOracle)
    {
      yield break;
    }

    if (outputOracle["abilities"] is not JsonArray abilities || abilities.Count == 0)
    {
      yield break;
    }

    // Split oracle text by newlines
    var lines = oracleText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

    // Parse each line to determine how many abilities it produces
    // This allows us to correctly group abilities from multi-keyword lines like "Flying, vigilance"
    var parser = new OracleParser();
    var lineNumber = 1;
    var abilityIndex = 0;

    foreach (var line in lines)
    {
      var trimmedLine = line.Trim();
      if (string.IsNullOrWhiteSpace(trimmedLine))
      {
        continue;
      }

      // Parse the line to see how many abilities it produces
      var parseResult = parser.Parse(trimmedLine);
      var expectedAbilityCount = parseResult.Output.Abilities.Count;

      if (expectedAbilityCount == 0 || abilityIndex >= abilities.Count)
      {
        // Skip lines that don't parse to abilities or if we've run out of expected abilities
        lineNumber++;
        continue;
      }

      // Collect the expected abilities for this line
      JsonNode expectedAbility;
      if (expectedAbilityCount == 1)
      {
        // Single ability - just use the ability directly
        expectedAbility = abilities[abilityIndex]!;
        abilityIndex++;
      }
      else
      {
        // Multiple abilities - create an array with deep copies to avoid parent conflicts
        var abilityGroup = new JsonArray();
        for (int i = 0; i < expectedAbilityCount && abilityIndex < abilities.Count; i++)
        {
          // Deep copy the node to avoid "node already has a parent" errors
          var abilityNode = abilities[abilityIndex];
          var copy = JsonNode.Parse(abilityNode!.ToJsonString());
          abilityGroup.Add(copy);
          abilityIndex++;
        }
        expectedAbility = abilityGroup;
      }

      yield return new OracleLineTestCase
      {
        Name = $"{cardTestCase.Name}/Line{lineNumber}",
        OracleText = trimmedLine,
        ExpectedAbility = expectedAbility,
        SourceCard = cardTestCase.Name,
        LineNumber = lineNumber,
        SourceFilePath = cardTestCase.FilePath,
      };

      lineNumber++;
    }
  }

  /// <summary>
  /// Gets test case data for NUnit's TestCaseSource.
  /// </summary>
  public static IEnumerable<TestCaseData> GetTestCaseData()
  {
    var loader = new OracleLineTestCaseLoader("HandParsedCards");
    foreach (var testCase in loader.GetAllTestCases())
    {
      yield return new TestCaseData(testCase).SetName($"OracleLines/{testCase.Name}");
    }
  }
}
