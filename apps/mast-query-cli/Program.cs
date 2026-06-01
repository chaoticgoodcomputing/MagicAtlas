using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST;
using MagicAST.Parsing;
using MagicAST.Query;
using MagicAST.Query.Patterns;
using MagicAST.Schema;

// mast-query CLI — parses the Scryfall oracle-cards bulk through the MagicAST parser, materialises
// each card's AST as canonical JSON, and runs a query fixture against the corpus (mast-query
// ADR-0001). This is the "queries run against the data" path: no Flowthru, no network — it reads
// the local bulk the triage flow already caches.
//
//   --bulk <path>     oracle-cards bulk (defaults to the cached triage copy)
//   --query <path>    query fixture (JSON: { name, pattern })
//   --dump "<name>"   parse one card and print its AST; skip the corpus run
//   --limit <n>       cap the corpus (for fast iteration)
//   --samples <n>     how many sample matches/unknowns to print (default 15)

string? bulkPath = null,
  queryPath = null,
  dumpName = null;
int? limit = null;
var samples = 15;
for (var i = 0; i < args.Length; i++)
{
  switch (args[i])
  {
    case "--bulk":
      bulkPath = args[++i];
      break;
    case "--query":
      queryPath = args[++i];
      break;
    case "--dump":
      dumpName = args[++i];
      break;
    case "--limit":
      limit = int.Parse(args[++i]);
      break;
    case "--samples":
      samples = int.Parse(args[++i]);
      break;
  }
}

bulkPath ??= Path.Combine(
  RepoRoot(),
  "tests",
  "magic-ast-tests",
  "Data",
  "_01_Raw",
  "Datasets",
  "External",
  "oracle-cards.json"
);

var snake = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
var indented = new JsonSerializerOptions(MagicASTJsonOptions.Strict) { WriteIndented = true };

Console.Error.WriteLine($"Loading bulk: {bulkPath}");
var raw = JsonSerializer.Deserialize<List<RawCard>>(File.ReadAllText(bulkPath), snake) ?? [];
Console.Error.WriteLine($"Bulk cards: {raw.Count}");

var parser = new CardParser();

if (dumpName is not null)
{
  var card =
    raw.FirstOrDefault(c => string.Equals(c.Name, dumpName, StringComparison.OrdinalIgnoreCase))
    ?? throw new InvalidOperationException($"Card not found in bulk: {dumpName}");
  Console.WriteLine(JsonSerializer.Serialize(parser.Parse(ToDto(card)).Output, indented));
  return;
}

var sw = System.Diagnostics.Stopwatch.StartNew();
var corpus = new List<CardDocument>();
int considered = 0,
  failures = 0;
foreach (var card in raw.Where(IsCommanderLegalPaper).Where(HasText))
{
  if (limit is not null && considered >= limit)
    break;
  considered++;
  try
  {
    var ast = parser.Parse(ToDto(card)).Output;
    var node = JsonSerializer.SerializeToNode(ast, MagicASTJsonOptions.Strict)!;
    corpus.Add(new CardDocument(card.Name, node));
  }
  catch
  {
    failures++;
  }
}
sw.Stop();
Console.Error.WriteLine(
  $"Parsed corpus: {corpus.Count} cards ({failures} parse/serialize failures) in {sw.ElapsedMilliseconds} ms"
);

if (queryPath is null)
{
  Console.Error.WriteLine("No --query given; corpus materialised only.");
  return;
}

var fixture = JsonNode.Parse(File.ReadAllText(queryPath))!;
var name = fixture["name"]!.GetValue<string>();
var pattern = PatternReader.Read(fixture["pattern"]!);
var result = new FilterAndVerifyEngine(SchemaExport.Build()).Run(name, pattern, corpus);

Console.WriteLine();
Console.WriteLine($"Query: {name}");
Console.WriteLine($"  corpus     {corpus.Count}");
Console.WriteLine($"  matched    {result.Matched.Count}");
Console.WriteLine($"  unknown    {result.Unknown.Count}");
Console.WriteLine($"  non-match  {result.NonMatch}");
var decidable = result.Matched.Count + result.NonMatch;
Console.WriteLine(
  $"  decidable  {decidable} ({(corpus.Count == 0 ? 0 : 100.0 * decidable / corpus.Count):F1}% — Unknown tracks MAST coverage)"
);
Console.WriteLine();
Console.WriteLine($"Sample matches:");
foreach (var m in result.Matched.Take(samples))
  Console.WriteLine($"  match  {m.Card}   @ {m.Path}");
Console.WriteLine();
Console.WriteLine($"Sample unknown:");
foreach (var m in result.Unknown.Take(samples))
  Console.WriteLine($"  ?      {m.Card}");

return;

static bool IsCommanderLegalPaper(RawCard c) =>
  c.Legalities is not null
  && c.Legalities.TryGetValue("commander", out var legality)
  && string.Equals(legality, "legal", StringComparison.OrdinalIgnoreCase)
  && c.Games is not null
  && c.Games.Contains("paper", StringComparer.OrdinalIgnoreCase);

static bool HasText(RawCard c) =>
  !string.IsNullOrWhiteSpace(c.OracleText) || (c.CardFaces?.Count ?? 0) > 0;

static CardInputDTO ToDto(RawCard c) =>
  new()
  {
    Id = c.Id,
    Name = c.Name,
    ManaCost = c.ManaCost,
    TypeLine = c.TypeLine,
    OracleText = c.OracleText,
    Power = c.Power,
    Toughness = c.Toughness,
    Loyalty = c.Loyalty,
    Colors = c.Colors,
    ColorIndicator = c.ColorIndicator,
    ColorIdentity = c.ColorIdentity,
    Keywords = c.Keywords,
    Layout = c.Layout,
    CardFaces = c
      .CardFaces?.Select(f => new CardFaceDTO
      {
        Name = f.Name,
        ManaCost = f.ManaCost,
        TypeLine = f.TypeLine,
        OracleText = f.OracleText,
        Power = f.Power,
        Toughness = f.Toughness,
        Loyalty = f.Loyalty,
        Colors = f.Colors,
        ColorIndicator = f.ColorIndicator,
      })
      .ToList(),
  };

static string RepoRoot()
{
  var d = new DirectoryInfo(AppContext.BaseDirectory);
  while (d is not null && !File.Exists(Path.Combine(d.FullName, "nx.json")))
    d = d.Parent;
  return d?.FullName
    ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above).");
}

/// <summary>Narrow Scryfall card projection (snake_case via the naming policy) — the fields the
/// MagicAST parser consumes, mirroring MastRawScryfallCard in the triage harness.</summary>
internal sealed record RawCard
{
  public string Id { get; init; } = "";
  public string Name { get; init; } = "";
  public string? ManaCost { get; init; }
  public string TypeLine { get; init; } = "";
  public string? OracleText { get; init; }
  public string? Power { get; init; }
  public string? Toughness { get; init; }
  public string? Loyalty { get; init; }
  public List<string>? Colors { get; init; }
  public List<string>? ColorIdentity { get; init; }
  public List<string>? ColorIndicator { get; init; }
  public List<string>? Keywords { get; init; }
  public string? Layout { get; init; }
  public Dictionary<string, string>? Legalities { get; init; }
  public List<string>? Games { get; init; }
  public List<RawFace>? CardFaces { get; init; }
}

internal sealed record RawFace
{
  public string Name { get; init; } = "";
  public string? ManaCost { get; init; }
  public string TypeLine { get; init; } = "";
  public string? OracleText { get; init; }
  public string? Power { get; init; }
  public string? Toughness { get; init; }
  public string? Loyalty { get; init; }
  public List<string>? Colors { get; init; }
  public List<string>? ColorIndicator { get; init; }
}
