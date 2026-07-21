namespace MagicAST.Tests.Tests;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Item R2 — the downstream BLAST-RADIUS report for <c>oracle-text-quarantine.json</c>.
/// <see cref="GoldOracleTextFidelityTests"/> only asserts pass/fail on a drifted/quarantined fixture; it
/// never says WHAT depends on it. This walks the SAME <c>Input.Name → fixturePath</c> join that report
/// uses (via <see cref="HandParsedTestCaseLoader"/>) and, for each quarantined fixture, computes which
/// combos in <c>tools/bench/MagicAtlas.Bench/combo-axis-expectations.json</c> name that card and which
/// interaction golds under <c>Fixtures/Interactions/golds/*.json</c> name it (their <c>cards[]</c> field)
/// — making "which pins/golds are at risk if this fixture's text is wrong" a GENERATED fact instead of
/// something a human cross-references by hand.
///
/// <para>
/// Deliberately NOT corpus-gated: unlike the fidelity test itself (which needs <c>card-inputs.json</c> /
/// <c>oracle-cards.json</c> to know what the AUTHORITATIVE text is), the blast radius only needs three
/// already-committed artifacts — the quarantine list, the gold corpus (for the name↔path join), the
/// pinned combos, and the interaction golds — so it runs identically on a fresh worktree checkout.
/// </para>
///
/// <para>
/// Diagnostic report, not a gate (mirrors <c>FilterCoverageReportTests</c>'s established pattern):
/// emits <c>Data/_08_Reporting/fidelity-blast-radius.json</c> (gitignored, regenerated on demand — same
/// convention as <c>combo-anchor-report.json</c> / <c>port-topology-demand.json</c>), run on demand via
/// <c>dotnet test --filter "Emits_fidelity_blast_radius_report"</c>.
/// </para>
/// </summary>
[TestFixture]
public class FidelityBlastRadiusReportTests
{
  [Test]
  [Explicit("Diagnostic corpus rollup; run on demand.")]
  public void Emits_fidelity_blast_radius_report()
  {
    var repoRoot = FindRepoRoot();

    var quarantine = LoadQuarantine(
      Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "oracle-text-quarantine.json")
    );
    Assert.That(quarantine, Is.Not.Empty, "oracle-text-quarantine.json is empty/missing — nothing to report.");

    // Input.Name -> fixture relative path (same shape oracle-text-quarantine.json's "card" field uses),
    // built by walking the SAME HandParsedCards tree GoldOracleTextFidelityTests validates against.
    var nameByFixture = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var testCase in HandParsedTestCaseLoader.GetAllTestCases())
    {
      var name = testCase.InputNode["Name"]?.ToString();
      if (name is not null)
        nameByFixture.TryAdd(testCase.Name.Replace('\\', '/'), name);
    }

    var combosByCard = LoadCombosByCard(
      Path.Combine(repoRoot, "tools", "bench", "MagicAtlas.Bench", "combo-axis-expectations.json")
    );
    var goldsByCard = LoadGoldsByCard(
      Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Interactions", "golds")
    );

    var entries = new List<BlastRadiusEntry>();
    foreach (var (fixture, tag, reason) in quarantine.OrderBy(q => q.Fixture, StringComparer.Ordinal))
    {
      var cardName = nameByFixture.GetValueOrDefault(fixture);
      var combos = cardName is not null
        ? combosByCard.GetValueOrDefault(cardName, [])
        : [];
      var golds = cardName is not null ? goldsByCard.GetValueOrDefault(cardName, []) : [];

      entries.Add(
        new BlastRadiusEntry(
          Fixture: fixture,
          Card: cardName,
          Tag: tag,
          Reason: reason,
          Combos: [.. combos.OrderBy(c => c.Id, StringComparer.Ordinal)],
          InteractionGolds: [.. golds.OrderBy(g => g, StringComparer.Ordinal)]
        )
      );
    }

    var withDownstream = entries.Count(e => e.Combos.Count > 0 || e.InteractionGolds.Count > 0);
    var totalComboHits = entries.Sum(e => e.Combos.Count);
    var totalGoldHits = entries.Sum(e => e.InteractionGolds.Count);

    TestContext.WriteLine(
      $"Quarantined fixtures: {entries.Count}  with downstream dependents: {withDownstream}  "
        + $"combo hits: {totalComboHits}  interaction-gold hits: {totalGoldHits}"
    );
    foreach (var e in entries.Where(e => e.Combos.Count > 0 || e.InteractionGolds.Count > 0))
      TestContext.WriteLine(
        $"  {e.Fixture} ('{e.Card}') -> combos: [{string.Join(", ", e.Combos.Select(c => $"{c.Id}:{c.Tier}"))}]"
          + $"  golds: [{string.Join(", ", e.InteractionGolds)}]"
      );

    var report = new BlastRadiusReport(
      GeneratedAt: "Tests/Parse/FidelityBlastRadiusReportTests",
      QuarantinedFixtureCount: entries.Count,
      WithDownstreamCount: withDownstream,
      Entries: entries
    );

    var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "_08_Reporting");
    Directory.CreateDirectory(outDir);
    var outPath = Path.Combine(outDir, "fidelity-blast-radius.json");
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    File.WriteAllText(outPath, JsonSerializer.Serialize(report, options) + "\n");
    TestContext.WriteLine($"Wrote {outPath}");
  }

  private sealed record ComboHit(string Id, string Tier);

  private sealed record BlastRadiusEntry(
    string Fixture,
    string? Card,
    string Tag,
    string Reason,
    IReadOnlyList<ComboHit> Combos,
    IReadOnlyList<string> InteractionGolds
  );

  private sealed record BlastRadiusReport(
    string GeneratedAt,
    int QuarantinedFixtureCount,
    int WithDownstreamCount,
    IReadOnlyList<BlastRadiusEntry> Entries
  );

  private static IReadOnlyList<(string Fixture, string Tag, string Reason)> LoadQuarantine(string path)
  {
    var list = new List<(string, string, string)>();
    if (!File.Exists(path))
      return list;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
      foreach (var e in entries.EnumerateArray())
      {
        var card = e.TryGetProperty("card", out var c) ? c.GetString() : null;
        if (card is null)
          continue;
        var tag = e.TryGetProperty("tag", out var t) ? t.GetString() ?? "" : "";
        var reason = e.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : "";
        list.Add((card, tag, reason));
      }

    return list;
  }

  // cardName -> the combos (id + DERIVED tier) on combo-axis-expectations.json whose cards[] names it.
  // The tier is not stored there any more (ADR 0004 §5): no axis exceptions and reconstructing = Green,
  // one or more exceptions = Amber, listed under `unreconstructed` = Missed.
  private static IReadOnlyDictionary<string, List<ComboHit>> LoadCombosByCard(string path)
  {
    var map = new Dictionary<string, List<ComboHit>>(StringComparer.Ordinal);
    if (!File.Exists(path))
      return map;

    var root = JsonNode.Parse(File.ReadAllText(path));
    if (root?["combos"] is not JsonArray combos)
      return map;

    var withExceptions = new HashSet<string>(StringComparer.Ordinal);
    if (root["axisExceptions"] is JsonArray exceptions)
      foreach (var e in exceptions)
        if (e?["combo"]?.ToString() is { } c)
          withExceptions.Add(c);

    var unreconstructed = new HashSet<string>(StringComparer.Ordinal);
    if (root["unreconstructed"] is JsonArray missed)
      foreach (var u in missed)
        if (u?["combo"]?.ToString() is { } c)
          unreconstructed.Add(c);

    foreach (var combo in combos)
    {
      var id = combo?["id"]?.ToString();
      if (id is null || combo?["cards"] is not JsonArray cards)
        continue;
      var tier = unreconstructed.Contains(id) ? "Missed"
        : withExceptions.Contains(id) ? "Amber"
        : "Green";
      foreach (var cardNode in cards)
      {
        var card = cardNode?.ToString();
        if (card is null)
          continue;
        (map.TryGetValue(card, out var list) ? list : map[card] = []).Add(new ComboHit(id, tier));
      }
    }

    return map;
  }

  // cardName -> the interaction gold ids (Fixtures/Interactions/golds/*.json) whose cards[] names it.
  private static IReadOnlyDictionary<string, List<string>> LoadGoldsByCard(string goldsDir)
  {
    var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    if (!Directory.Exists(goldsDir))
      return map;

    foreach (var file in Directory.EnumerateFiles(goldsDir, "*.json", SearchOption.TopDirectoryOnly))
    {
      JsonNode? root;
      try
      {
        root = JsonNode.Parse(File.ReadAllText(file));
      }
      catch
      {
        continue;
      }

      var id = root?["id"]?.ToString();
      if (id is null || root?["cards"] is not JsonArray cards)
        continue;
      foreach (var cardNode in cards)
      {
        var card = cardNode?.ToString();
        if (card is null)
          continue;
        (map.TryGetValue(card, out var list) ? list : map[card] = []).Add(id);
      }
    }

    return map;
  }

  // Walks up from the test directory to the repo root (the dir holding tests/magic-ast-tests's csproj) —
  // same idiom GoldRegenerationUtility uses — so this reads combo-axis-expectations.json from the SOURCE
  // tree (a sibling project), not a build-output copy that doesn't exist for a cross-project artifact.
  private static string FindRepoRoot()
  {
    var projRel = Path.Combine("tests", "magic-ast-tests", "MagicAtlas.Ast.Tests.csproj");
    var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, projRel)))
      dir = dir.Parent;
    Assert.That(dir, Is.Not.Null, "Could not locate the repo root from the test directory.");
    return dir!.FullName;
  }
}
