namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;
using MagicAST.AST.References;

/// <summary>
/// Loads <c>ObjectFilter</c> <c>Subsumes</c> conformance cases from
/// <c>Data/FilterRelations/subsumes/*.json</c> — each
/// <c>{ "Sub": &lt;ObjectFilter&gt;, "Sup": &lt;ObjectFilter&gt;, "Expected": "Yes|No|Unknown" }</c>
/// — for NUnit's <c>TestCaseSource</c>.
/// </summary>
public static class SubsumptionTestCaseLoader
{
  private static string Directory() =>
    Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "FilterRelations", "subsumes");

  public static IEnumerable<SubsumptionTestCase> GetAllTestCases()
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
      var document =
        JsonNode.Parse(File.ReadAllText(filePath))
        ?? throw new InvalidOperationException($"Failed to parse JSON from {filePath}");

      var sub = document["Sub"] ?? throw new InvalidOperationException($"Missing 'Sub' in {filePath}");
      var sup = document["Sup"] ?? throw new InvalidOperationException($"Missing 'Sup' in {filePath}");
      var expected =
        (string?)document["Expected"]
        ?? throw new InvalidOperationException($"Missing 'Expected' in {filePath}");

      yield return new SubsumptionTestCase
      {
        Name = Path.GetFileNameWithoutExtension(filePath),
        Sub = sub,
        Sup = sup,
        Expected = Enum.Parse<Trilean>(expected),
      };
    }
  }

  public static IEnumerable<TestCaseData> GetTestCaseData()
  {
    foreach (var testCase in GetAllTestCases())
    {
      yield return new TestCaseData(testCase).SetName($"Subsumes/{testCase.Name}");
    }
  }
}
