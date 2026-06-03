namespace MagicAST.Tests.Infrastructure;

using System.Text.Json.Nodes;
using MagicAST.AST.References;

/// <summary>
/// Loads <c>ObjectFilter</c> <c>Intersects</c> conformance cases from
/// <c>Data/FilterRelations/intersects/*.json</c> — each
/// <c>{ "A": &lt;ObjectFilter&gt;, "B": &lt;ObjectFilter&gt;, "Expected": "Overlaps|Disjoint|Unknown" }</c>
/// — and exposes them to NUnit's <c>TestCaseSource</c>. Mirrors
/// <see cref="KeywordExpansionTestCaseLoader"/>.
/// </summary>
public static class FilterRelationTestCaseLoader
{
  private static string Directory() =>
    Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "FilterRelations",
      "intersects"
    );

  public static IEnumerable<FilterRelationTestCase> GetAllTestCases()
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

      var a = document["A"] ?? throw new InvalidOperationException($"Missing 'A' in {filePath}");
      var b = document["B"] ?? throw new InvalidOperationException($"Missing 'B' in {filePath}");
      var expected =
        (string?)document["Expected"]
        ?? throw new InvalidOperationException($"Missing 'Expected' in {filePath}");

      yield return new FilterRelationTestCase
      {
        Name = Path.GetFileNameWithoutExtension(filePath),
        A = a,
        B = b,
        Expected = Enum.Parse<FilterRelation>(expected),
      };
    }
  }

  public static IEnumerable<TestCaseData> GetTestCaseData()
  {
    foreach (var testCase in GetAllTestCases())
    {
      yield return new TestCaseData(testCase).SetName($"FilterRelations/{testCase.Name}");
    }
  }
}
