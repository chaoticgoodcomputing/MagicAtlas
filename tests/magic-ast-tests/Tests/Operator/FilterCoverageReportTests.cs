namespace MagicAST.Tests.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Analysis;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Diagnostic, not part of the green gate: harvest every <see cref="ObjectFilter"/> from the gold
/// corpus, run the relation operators over all pairs, and emit the coverage-pressure rollup — which
/// axes and Phase-3 relational gaps most often force <c>Unknown</c> (i.e. what to build/parse next).
/// Run on demand: <c>dotnet test --filter "Emits_filter_coverage_report"</c>.
/// </summary>
[TestFixture]
public class FilterCoverageReportTests
{
  [Test]
  [Explicit("Diagnostic corpus rollup; run on demand.")]
  public void Emits_filter_coverage_report()
  {
    var ontology =
      JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(TestData.OntologyPath))
      ?? throw new InvalidOperationException("ontology");

    var filters = new List<ObjectFilter>();
    foreach (var testCase in HandParsedTestCaseLoader.GetAllTestCases())
    {
      try
      {
        filters.AddRange(ObjectFilterCollector.Collect(testCase.GetOutput()));
      }
      catch
      {
        // A fixture that won't deserialize is not this diagnostic's concern.
      }
    }

    var report = FilterCoverage.Analyze(filters, ontology);

    TestContext.WriteLine(
      $"Filters: {report.FilterCount}  Pairs: {report.PairCount}  Capped: {report.Capped}"
    );
    TestContext.WriteLine(
      "Intersect verdicts: "
        + string.Join(", ", report.IntersectVerdicts.Select(kv => $"{kv.Key}={kv.Value}"))
    );
    TestContext.WriteLine("Intersect Unknown reasons (ranked):");
    foreach (var t in report.IntersectUnknownReasons)
      TestContext.WriteLine($"  {t.Axis}: {t.Count}");
    TestContext.WriteLine(
      "Subsume verdicts: "
        + string.Join(", ", report.SubsumeVerdicts.Select(kv => $"{kv.Key}={kv.Value}"))
    );
    TestContext.WriteLine("Subsume No/Unknown reasons (ranked):");
    foreach (var t in report.SubsumeOpenReasons)
      TestContext.WriteLine($"  {t.Axis}: {t.Count}");
    TestContext.WriteLine("Relational-axis demand (filters constraining each Phase-3 axis):");
    foreach (var t in report.RelationalAxisFrequency)
      TestContext.WriteLine($"  {t.Axis}: {t.Count}");

    var outPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "filter-coverage-report.json");
    File.WriteAllText(
      outPath,
      JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true })
    );
    TestContext.WriteLine($"Wrote {outPath}");
  }
}
