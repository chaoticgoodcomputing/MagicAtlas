namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;

/// <summary>
/// Generic loader for card test cases from a specified directory.
/// </summary>
public sealed class CardTestCaseLoader
{
  private readonly string _testCasesDirectory;
  private readonly string _categoryName;

  /// <summary>
  /// Creates a loader for the specified data subdirectory.
  /// </summary>
  /// <param name="dataSubdirectory">Subdirectory name under Data/ (e.g., "HandParsedCards")</param>
  public CardTestCaseLoader(string dataSubdirectory)
  {
    _categoryName = dataSubdirectory;
    _testCasesDirectory = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Data",
      dataSubdirectory
    );
  }

  /// <summary>
  /// Gets all available test cases from the directory.
  /// </summary>
  public IEnumerable<CardTestCase> GetAllTestCases()
  {
    if (!Directory.Exists(_testCasesDirectory))
    {
      yield break;
    }

    foreach (
      var filePath in Directory.EnumerateFiles(
        _testCasesDirectory,
        "*.json",
        SearchOption.AllDirectories
      )
    )
    {
      yield return LoadTestCase(filePath);
    }
  }

  /// <summary>
  /// Loads a single test case from a JSON file.
  /// </summary>
  private CardTestCase LoadTestCase(string filePath)
  {
    var json = File.ReadAllText(filePath);
    var document =
      JsonNode.Parse(json)
      ?? throw new InvalidOperationException($"Failed to parse JSON from {filePath}");

    var inputNode =
      document["input"]
      ?? throw new InvalidOperationException($"Missing 'input' property in {filePath}");

    var outputNode =
      document["output"]
      ?? throw new InvalidOperationException($"Missing 'output' property in {filePath}");

    // Use relative path from data directory as the test name
    var relativePath = Path.GetRelativePath(_testCasesDirectory, filePath);
    var name = Path.ChangeExtension(relativePath, null); // Remove .json extension

    return new CardTestCase
    {
      Name = name,
      FilePath = filePath,
      InputNode = inputNode,
      OutputNode = outputNode,
    };
  }

  /// <summary>
  /// Gets test case data for NUnit's TestCaseSource.
  /// </summary>
  public IEnumerable<TestCaseData> GetTestCaseData()
  {
    foreach (var testCase in GetAllTestCases())
    {
      yield return new TestCaseData(testCase).SetName($"{_categoryName}/{testCase.Name}");
    }
  }
}
