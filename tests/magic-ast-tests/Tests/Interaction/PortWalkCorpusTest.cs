namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// The <see cref="PortWalk"/> analogue of <c>CorpusProjectionTest</c> (S3b): run the new walk over the
/// <b>whole</b> hand-parsed gold corpus and assert <b>totality</b> — projecting a real card must never
/// throw. The walk deserializes captured sub-nodes (filters, token specs, mana symbols) and dispatches
/// on node kind with coarse fallbacks, so this sweep is where a long-tail AST shape would surface.
/// </summary>
[TestFixture]
public class PortWalkCorpusTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static string CorpusDir() =>
    Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "HandParsedCards");

  [Test]
  public void Walks_every_corpus_card_without_throwing()
  {
    var walk = new PortWalk(Ontology);
    var files = Directory
      .EnumerateFiles(CorpusDir(), "*.json", SearchOption.AllDirectories)
      .ToList();
    Assert.That(files.Count, Is.GreaterThan(500), "expected the full hand-parsed corpus");

    var failures = new List<string>();
    var withPorts = 0;
    foreach (var file in files)
    {
      try
      {
        var gold = JsonNode.Parse(File.ReadAllText(file));
        var abilities = gold?["Output"]?["Oracle"]?["Abilities"];
        var name = gold?["Output"]?["Name"]?.ToString() ?? Path.GetFileNameWithoutExtension(file);
        if (walk.Project(name, abilities).Ports.Count > 0)
          withPorts++;
      }
      catch (Exception ex)
      {
        failures.Add($"{Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
      }
    }

    TestContext.Out.WriteLine($"corpus cards: {files.Count}, cards yielding >=1 port: {withPorts}");
    Assert.That(
      failures,
      Is.Empty,
      "PortWalk threw on real cards:\n" + string.Join("\n", failures.Take(15))
    );
  }
}
