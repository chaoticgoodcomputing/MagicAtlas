namespace MagicAST.Tests.Tests;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Maintenance utility (not a behavioral test — marked <c>[Explicit]</c>): SURGICALLY injects the
/// newly-serialized per-ability <c>SourceSpan</c>/<c>OracleLineIndex</c> provenance keys into each
/// hand-parsed gold fixture, leaving every other key, value, and formatting decision untouched.
///
/// <para>
/// Why not a full re-serialize (as <see cref="GoldRegenerationUtility"/> does)? Because re-parsing and
/// re-emitting the whole card normalises a lot of pre-existing, test-tolerated drift — omitted
/// default-valued keys (<c>IsVariable:false</c>, …), key ordering, and the compact-array formatting some
/// golds were authored in. That would bury the span change in thousands of lines of unrelated churn and
/// risk masking a real regression. Instead this loads each gold's own <c>Output</c> node and, walking it
/// in lockstep with the current parser's output, copies ONLY <c>SourceSpan</c> and <c>OracleLineIndex</c>
/// onto the matching ability objects. The parser logic is unchanged, so the resulting diff is exactly
/// "the same gold, now carrying the two provenance keys".
/// </para>
/// </summary>
[TestFixture]
[Explicit("Maintenance utility: injects SourceSpan/OracleLineIndex into gold Outputs in place. Run on demand.")]
public class GoldSpanReserializeUtility
{
  [Test]
  public void Inject_span_provenance_into_golds()
  {
    var projRel = Path.Combine("tests", "magic-ast-tests", "MagicAtlas.Ast.Tests.csproj");
    var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, projRel)))
    {
      dir = dir.Parent;
    }
    Assert.That(dir, Is.Not.Null, "Could not locate the repo root from the test directory.");
    var fixturesRoot = Path.Combine(dir!.FullName, "tests", "magic-ast-tests", "Fixtures");
    var goldRoots = new[]
    {
      Path.Combine(fixturesRoot, "HandParsedCards"),
      Path.Combine(fixturesRoot, "MalformedParsedCards"),
    };
    foreach (var r in goldRoots)
    {
      Assert.That(Directory.Exists(r), Is.True, $"Gold root missing: {r}");
    }

    var writeOpts = new JsonSerializerOptions(MagicASTJsonOptions.Strict)
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    var parser = new CardParser();

    var files = goldRoots
      .SelectMany(r => Directory.EnumerateFiles(r, "*.json", SearchOption.AllDirectories))
      .OrderBy(p => p, StringComparer.Ordinal)
      .ToList();

    var changed = 0;
    var conflicts = new List<string>();
    var skipped = new List<string>();

    foreach (var path in files)
    {
      var root = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
      if (root?["Input"] is not JsonObject inputObj || root["Output"] is not JsonObject outputGold)
      {
        skipped.Add($"SKIP {path}: no Input/Output object");
        continue;
      }

      var dto = inputObj.Deserialize<CardInputDTO>(MagicASTJsonOptions.Strict);
      if (dto is null)
      {
        skipped.Add($"SKIP {path}: Input did not deserialize");
        continue;
      }

      var result = parser.Parse(dto);
      var canonical =
        JsonSerializer.SerializeToNode(result.Output, MagicASTJsonOptions.Strict) as JsonObject;
      if (canonical is null)
      {
        skipped.Add($"SKIP {path}: parser output did not serialize to object");
        continue;
      }

      var fileConflicts = new List<string>();
      InjectSpans(outputGold, canonical, path, fileConflicts);
      conflicts.AddRange(fileConflicts);

      var after = root.ToJsonString(writeOpts) + "\n";
      if (File.ReadAllText(path) != after)
      {
        File.WriteAllText(path, after);
        changed++;
      }
    }

    TestContext.Out.WriteLine($"Injected span provenance; {changed}/{files.Count} files written.");
    if (conflicts.Count > 0)
    {
      TestContext.Out.WriteLine($"CONFLICTS ({conflicts.Count}) — existing SourceSpan differed from parser:");
      TestContext.Out.WriteLine(string.Join("\n", conflicts.Take(50)));
    }
    if (skipped.Count > 0)
    {
      TestContext.Out.WriteLine(string.Join("\n", skipped));
    }

    Assert.That(
      conflicts,
      Is.Empty,
      "Some golds had an existing SourceSpan that disagreed with the parser — investigate before committing."
    );
  }

  /// <summary>
  /// Walk <paramref name="gold"/> in lockstep with <paramref name="canon"/> (the current parser's
  /// serialized output for the same card). Whenever the canonical node marks an ability (it carries an
  /// <c>OracleLineIndex</c> — a property unique to <see cref="MagicAST.AST.Abilities.Ability"/>), copy
  /// <c>SourceSpan</c> and <c>OracleLineIndex</c> onto the gold object. Recurse only through keys/indices
  /// present in gold, so canonical-only keys (e.g. reparse-emitted default fields) are never introduced.
  /// </summary>
  private static void InjectSpans(JsonNode? gold, JsonNode? canon, string path, List<string> conflicts)
  {
    if (gold is JsonObject go && canon is JsonObject co)
    {
      if (co.ContainsKey("OracleLineIndex"))
      {
        if (co["SourceSpan"] is JsonNode span)
        {
          if (go["SourceSpan"] is JsonNode existing
            && !JsonComparer.AreEqual(existing, span))
          {
            conflicts.Add($"{path}: SourceSpan {existing.ToJsonString()} != parser {span.ToJsonString()}");
          }
          go["SourceSpan"] = span.DeepClone();
        }
        go["OracleLineIndex"] = co["OracleLineIndex"]!.DeepClone();
      }

      foreach (var kvp in go)
      {
        if (co.TryGetPropertyValue(kvp.Key, out var cv))
        {
          InjectSpans(kvp.Value, cv, path, conflicts);
        }
      }
    }
    else if (gold is JsonArray ga && canon is JsonArray ca)
    {
      var n = Math.Min(ga.Count, ca.Count);
      for (var i = 0; i < n; i++)
      {
        InjectSpans(ga[i], ca[i], path, conflicts);
      }
    }
  }
}
