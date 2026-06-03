namespace MagicAST.Query.Tests;

using System.Text.Json.Nodes;
using MagicAST.Query;
using MagicAST.Query.Patterns;
using MagicAST.Schema;

/// <summary>
/// The mast-query conformance suite (ADR-0001): each fixture pairs a query pattern with the
/// determinacy it must produce for each card in the frozen reference corpus. A failing fixture is
/// how parser drift or an engine regression surfaces — loudly. The engine is constructed from
/// MAST's live schema export (ADR-0008), the same contract a Python engine would bind to.
/// </summary>
[TestFixture]
public class ConformanceTests
{
  private static readonly string DataDir = Path.Combine(
    Path.GetDirectoryName(typeof(ConformanceTests).Assembly.Location)!,
    "Data",
    "Query"
  );

  private static readonly AstSchema Schema = SchemaExport.Build();

  private static IReadOnlyList<CardDocument> LoadCorpus()
  {
    var dir = Path.Combine(DataDir, "ReferenceCorpus");
    var docs = new List<CardDocument>();
    foreach (
      var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal)
    )
    {
      var node =
        JsonNode.Parse(File.ReadAllText(file))
        ?? throw new InvalidOperationException($"Empty corpus file: {file}");
      var name = node["Name"]!.GetValue<string>();
      docs.Add(new CardDocument(name, node));
    }
    return docs;
  }

  private static IEnumerable<TestCaseData> Fixtures()
  {
    var dir = Path.Combine(DataDir, "Conformance");
    foreach (
      var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal)
    )
      yield return new TestCaseData(file).SetName(
        $"Conformance({Path.GetFileNameWithoutExtension(file)})"
      );
  }

  [TestCaseSource(nameof(Fixtures))]
  public void Fixture_reproduces_expected_determinacy(string fixturePath)
  {
    var fixture = JsonNode.Parse(File.ReadAllText(fixturePath))!;
    var name = fixture["name"]!.GetValue<string>();
    var pattern = PatternReader.Read(fixture["pattern"]!);
    var expected = fixture["expected"]!.AsObject();

    var result = new FilterAndVerifyEngine(Schema).Run(name, pattern, LoadCorpus());

    var byCard = new Dictionary<string, Determinacy>(StringComparer.Ordinal);
    foreach (var m in result.Matched)
      byCard[m.Card] = m.Determinacy;
    foreach (var m in result.Unknown)
      byCard[m.Card] = m.Determinacy;

    foreach (var entry in expected)
    {
      var want = Enum.Parse<Determinacy>(entry.Value!.GetValue<string>());
      var got = byCard.TryGetValue(entry.Key, out var d) ? d : Determinacy.NoMatch;
      Assert.That(got, Is.EqualTo(want), $"{name}: card '{entry.Key}'");
    }
  }

  [Test]
  public void Burn_spot_binds_the_victim_capture_on_a_structural_match()
  {
    var fixture = JsonNode.Parse(
      File.ReadAllText(Path.Combine(DataDir, "Conformance", "burn-spot.json"))
    )!;
    var pattern = PatternReader.Read(fixture["pattern"]!);

    var result = new FilterAndVerifyEngine(Schema).Run("burn-spot", pattern, LoadCorpus());

    var bolt = result.Matched.Single(m => m.Card == "Lightning Bolt");
    Assert.Multiple(() =>
    {
      Assert.That(bolt.Provenance, Is.EqualTo(Provenance.Structural));
      Assert.That(bolt.Captures, Is.Not.Null);
      Assert.That(bolt.Captures!.ContainsKey("victim"), Is.True);
    });
  }
}
