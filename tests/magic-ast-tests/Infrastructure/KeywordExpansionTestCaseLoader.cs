namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;

/// <summary>
/// Loads keyword-expansion gold cases from <c>Data/KeywordExpansions/*.json</c> and
/// exposes them to NUnit's <c>TestCaseSource</c>. Each file is
/// <c>{ "Keyword", "Parameter"?, "Expected" }</c> — see
/// <see cref="KeywordExpansionTestCase"/>.
/// </summary>
public static class KeywordExpansionTestCaseLoader
{
  private const string Subdirectory = "KeywordExpansions";

  private static string Directory() =>
    Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", Subdirectory);

  /// <summary>All keyword-expansion gold cases under <c>Data/KeywordExpansions</c>.</summary>
  public static IEnumerable<KeywordExpansionTestCase> GetAllTestCases()
  {
    var dir = Directory();
    if (!System.IO.Directory.Exists(dir))
    {
      yield break;
    }

    foreach (
      var filePath in System.IO.Directory.EnumerateFiles(
        dir,
        "*.json",
        SearchOption.AllDirectories
      )
    )
    {
      yield return Load(dir, filePath);
    }
  }

  private static KeywordExpansionTestCase Load(string dir, string filePath)
  {
    var json = File.ReadAllText(filePath);
    var document =
      JsonNode.Parse(json)
      ?? throw new InvalidOperationException($"Failed to parse JSON from {filePath}");

    var keyword =
      (string?)document["Keyword"]
      ?? throw new InvalidOperationException($"Missing 'Keyword' property in {filePath}");

    var parameter = (string?)document["Parameter"];

    var expected =
      document["Expected"]
      ?? throw new InvalidOperationException($"Missing 'Expected' property in {filePath}");

    var relativePath = Path.GetRelativePath(dir, filePath);
    var name = Path.ChangeExtension(relativePath, null);

    return new KeywordExpansionTestCase
    {
      Name = name,
      FilePath = filePath,
      Keyword = keyword,
      Parameter = parameter,
      ExpectedNode = expected,
    };
  }

  /// <summary>Test-case data for NUnit's <c>TestCaseSource</c>.</summary>
  public static IEnumerable<TestCaseData> GetTestCaseData()
  {
    foreach (var testCase in GetAllTestCases())
    {
      yield return new TestCaseData(testCase).SetName($"KeywordExpansions/{testCase.Name}");
    }
  }
}
