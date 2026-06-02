namespace MagicAST.Interaction.Tests;

using System.Text.Json.Nodes;
using MagicAST.Interaction;
using MagicAST.Schema;

/// <summary>
/// The projector half of the rollout-C4 fleet: run <see cref="PortProjector"/> over the <b>whole</b>
/// hand-parsed gold corpus and assert <b>totality</b> — projecting a real card must never throw. The
/// projector translates captured nodes into <c>ObjectFilter</c>s / token specs, and that
/// deserialization is the one place a long-tail card shape could blow up; the operator soundness
/// fleet (OperatorPropertyTests, magic-ast-tests) covers the verdict half of the bar. Reads the
/// source corpus directly by walking up to it, so no 950-file copy is vendored here.
/// </summary>
[TestFixture]
public class CorpusProjectionTest
{
  private static readonly AstSchema Schema = SchemaExport.Build();

  private static string FindCorpusDir()
  {
    for (
      var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
      dir is not null;
      dir = dir.Parent
    )
    {
      var candidate = Path.Combine(
        dir.FullName,
        "tests",
        "magic-ast-tests",
        "Data",
        "HandParsedCards"
      );
      if (Directory.Exists(candidate))
        return candidate;
    }
    throw new DirectoryNotFoundException(
      "HandParsedCards corpus not found above " + TestContext.CurrentContext.TestDirectory
    );
  }

  [Test]
  public void Projects_every_corpus_card_without_throwing()
  {
    var projector = new PortProjector(Schema);
    var files = Directory
      .EnumerateFiles(FindCorpusDir(), "*.json", SearchOption.AllDirectories)
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
        if (projector.Project(name, abilities).Count > 0)
          withPorts++;
      }
      catch (Exception ex)
      {
        failures.Add($"{Path.GetFileName(file)}: {ex.GetType().Name}: {ex.Message}");
      }
    }

    TestContext.WriteLine($"corpus cards: {files.Count}, cards yielding ≥1 port: {withPorts}");
    Assert.That(
      failures,
      Is.Empty,
      "projector threw on real cards:\n" + string.Join("\n", failures.Take(15))
    );
  }
}
