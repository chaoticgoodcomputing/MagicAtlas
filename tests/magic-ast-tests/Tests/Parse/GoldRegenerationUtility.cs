namespace MagicAST.Tests.Tests;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST;
using MagicAST.Parsing;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Maintenance utility (not a behavioral test — marked <c>[Explicit]</c>): re-points the golds listed
/// in <c>MAST_REGEN_LIST</c> to their real card by lifting <c>Input</c> from the corpus
/// (<c>card-inputs.json</c>) and regenerating a draft <c>Output</c> from the current parser.
///
/// <para>
/// This is the <b>bootstrap step</b> of the gold-fidelity cleanup TDD loop (see
/// <c>libs/magic-ast/docs/gold-oracle-fidelity-cleanup.md</c>). It produces a self-consistent draft —
/// it parses the exact 8-field <c>Input</c> it writes — so the draft asserts "the parser does this on
/// the real card", NOT "this is correct". The draft is then judged: a PASS draft (parser already
/// correct ⇒ snapshot ≡ hand-authored) is kept and de-quarantined; a FAIL draft is a red gold driving
/// a parser fix, after which the card is re-bootstrapped and re-judged. Never commit a draft unjudged.
/// </para>
/// </summary>
[TestFixture]
[Explicit("Maintenance utility: re-points listed golds to the corpus + regenerates AST. Run on demand.")]
public class GoldRegenerationUtility
{
  private static readonly string[] GoldInputFields =
  [
    "Name",
    "ManaCost",
    "TypeLine",
    "OracleText",
    "Power",
    "Toughness",
    "Colors",
    "ColorIdentity",
  ];

  [Test]
  public void Regenerate_listed_golds_from_corpus()
  {
    var listPath =
      Environment.GetEnvironmentVariable("MAST_REGEN_LIST") ?? "/tmp/golds-to-regen.txt";
    var rels = File.ReadAllLines(listPath)
      .Select(l => l.Trim())
      .Where(l => l.Length > 0)
      .ToList();

    // The build output is nx-centralized under dist/, not beside the .csproj — so walk up to the repo
    // root (the dir holding tests/magic-ast-tests/MagicAtlas.Ast.Tests.csproj) and write the SOURCE
    // golds, not the bin copies.
    var projRel = Path.Combine("tests", "magic-ast-tests", "MagicAtlas.Ast.Tests.csproj");
    var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, projRel)))
    {
      dir = dir.Parent;
    }
    Assert.That(dir, Is.Not.Null, "Could not locate the repo root from the test directory.");
    var goldRoot = Path.Combine(dir!.FullName, "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");

    var corpus = LoadCorpusInputs();
    Assert.That(corpus, Is.Not.Null.And.Not.Empty, "card-inputs.json missing or empty — run InteractionTriage.");

    var writeOpts = new JsonSerializerOptions(MagicASTJsonOptions.Strict)
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    var parser = new CardParser();
    var report = new List<string>();

    foreach (var rel in rels)
    {
      var goldPath = Path.Combine(goldRoot, rel.Replace('/', Path.DirectorySeparatorChar) + ".json");
      if (!File.Exists(goldPath))
      {
        report.Add($"MISS {rel}: gold file not found");
        continue;
      }

      var goldNode = JsonNode.Parse(File.ReadAllText(goldPath))!.AsObject();
      var name = goldNode["Input"]?["Name"]?.ToString();
      // Prefer the corpus record (re-points the gold to current corpus text); fall back to the gold's OWN
      // Input for a self-contained hand-parsed gold not in the combo corpus (e.g. a Saga used only as a
      // parser fixture) — those are still regenerable from the input they already carry.
      JsonObject? source =
        name is not null && corpus!.TryGetValue(name, out var cinput) ? cinput
        : goldNode["Input"] as JsonObject;
      if (source is null)
      {
        report.Add($"SKIP {rel}: '{name}' has no input (not in corpus, no gold Input)");
        continue;
      }

      // Build the canonical 8-field gold Input (drop Keywords/Id/Layout to match the existing gold
      // convention; the snapshot stays self-consistent because we parse THIS).
      var inputObj = new JsonObject();
      foreach (var key in GoldInputFields)
      {
        if (source[key] is JsonNode v)
        {
          inputObj[key] = v.DeepClone();
        }
      }

      var dto = inputObj.Deserialize<CardInputDTO>(MagicASTJsonOptions.Strict)!;
      var result = parser.Parse(dto);
      var outputNode = JsonSerializer.SerializeToNode(result.Output, MagicASTJsonOptions.Strict);

      var newGold = new JsonObject { ["Input"] = inputObj, ["Output"] = outputNode };
      File.WriteAllText(goldPath, newGold.ToJsonString(writeOpts) + "\n");
      report.Add($"OK   {rel}: {name}");
    }

    TestContext.Out.WriteLine($"Regenerated {report.Count(r => r.StartsWith("OK"))}/{rels.Count}:");
    TestContext.Out.WriteLine(string.Join("\n", report));
  }

  // name -> the gold Input (PascalCase 8-field shape). Primary source is the commander-legal-paper
  // corpus (card-inputs.json); the full Scryfall bulk (oracle-cards.json) fills gaps for cards filtered
  // OUT of the corpus (digital-only/Alchemy, non-commander) — the authoritative SUPERSET the fidelity
  // test now validates against. Corpus wins on conflict (it is the exact text the parser consumes).
  private static Dictionary<string, JsonObject>? LoadCorpusInputs()
  {
    var dict = new Dictionary<string, JsonObject>(StringComparer.Ordinal);

    var corpusPath = TestData.CardInputsPath;
    if (File.Exists(corpusPath))
    {
      foreach (var rec in JsonNode.Parse(File.ReadAllText(corpusPath))!.AsArray())
      {
        if (rec?["Input"] is not JsonObject input)
        {
          continue;
        }

        var name = input["Name"]?.ToString();
        if (name is not null && !dict.ContainsKey(name))
        {
          dict[name] = input;
        }
      }
    }

    var bulkPath = TestData.OracleCardsPath;
    if (File.Exists(bulkPath))
    {
      foreach (var rec in JsonNode.Parse(File.ReadAllText(bulkPath))!.AsArray())
      {
        var name = rec?["name"]?.ToString();
        if (name is not null && !dict.ContainsKey(name)) // corpus wins
        {
          dict[name] = BulkToGoldInput(rec!.AsObject());
        }
      }
    }

    return dict.Count == 0 ? null : dict;
  }

  // Maps a raw Scryfall bulk record (snake_case) onto the PascalCase gold Input shape the regenerator
  // reads. Single-faced only (the 8 gold fields carry no faces) — adequate for corpus-absent normal
  // cards; a DFC absent from the corpus would need the face-join handled here.
  private static JsonObject BulkToGoldInput(JsonObject bulk)
  {
    var input = new JsonObject();
    foreach (var (gold, snake) in BulkFieldMap)
    {
      if (bulk[snake] is JsonNode v)
      {
        input[gold] = v.DeepClone();
      }
    }
    return input;
  }

  private static readonly (string Gold, string Snake)[] BulkFieldMap =
  [
    ("Name", "name"),
    ("ManaCost", "mana_cost"),
    ("TypeLine", "type_line"),
    ("OracleText", "oracle_text"),
    ("Power", "power"),
    ("Toughness", "toughness"),
    ("Colors", "colors"),
    ("ColorIdentity", "color_identity"),
  ];
}
