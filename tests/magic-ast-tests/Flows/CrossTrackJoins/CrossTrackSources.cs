namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// The I/O half of the ADR-0004 §4 joins: loaders that turn the committed artifacts of each track into
/// the value inputs <see cref="CrossTrackJoiner"/> consumes. Kept strictly separate from the joining so
/// the joins themselves can be driven from hermetic reconstructed inputs (the Suture-Priest shape) with
/// no filesystem involved.
///
/// <para>Every source here is <b>already committed</b> — the quarantine, the parse golds, the pins, the
/// interaction golds, the rollup, the engine sources — so both joins run identically on a clean checkout
/// with no corpus present. That is deliberate: a join that only runs when the corpus happens to be
/// downloaded is a join that silently does not run.</para>
/// </summary>
public static class CrossTrackSources
{
  public const string QuarantineRelPath = "tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json";
  public const string HandParsedCardsRelPath = "tests/magic-ast-tests/Fixtures/HandParsedCards";
  public const string InteractionGoldsRelPath = "tests/magic-ast-tests/Fixtures/Interactions/golds";
  public const string RollupCitedRelPath = "tests/magic-ast-tests/Fixtures/Interactions/rollup/port-interactions.cited.json";
  public const string ExpectedTiersRelPath = "tools/bench/MagicAtlas.Bench/combo-expected-tiers.json";
  public const string AcknowledgedRelPath = "tools/bench/MagicAtlas.Bench/fidelity-risk-acknowledged.json";

  /// <summary>The engine sources scanned for rule-id references (the rule → code leg of §2).</summary>
  public const string EngineSourcesRelPath = "libs";

  private static readonly string[] RuleSections = ["polarity", "match_policy", "guards", "bridges"];

  /// <summary>Walks up to the workspace root (the directory holding <c>nx.json</c>) — the same marker
  /// <c>FlowProbes</c> uses, so a test binary and a <c>dotnet run --flow</c> resolve identically.</summary>
  public static string RepoRoot(string? from = null)
  {
    var dir = new DirectoryInfo(from ?? AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate the workspace root (no nx.json above " + (from ?? AppContext.BaseDirectory) + ").");
  }

  // ── Parse track ──────────────────────────────────────────────────────────────────────────────────

  /// <summary>The quarantine entries (Parse track).</summary>
  public static IReadOnlyList<CrossTrackJoiner.QuarantinedFixture> LoadQuarantine(string repoRoot)
  {
    var path = Path.Combine(repoRoot, QuarantineRelPath);
    var list = new List<CrossTrackJoiner.QuarantinedFixture>();
    if (!File.Exists(path))
      return list;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (!doc.RootElement.TryGetProperty("entries", out var entries))
      return list;

    foreach (var e in entries.EnumerateArray())
    {
      if (!e.TryGetProperty("card", out var c) || c.GetString() is not { } fixture)
        continue;
      list.Add(
        new CrossTrackJoiner.QuarantinedFixture(
          fixture,
          e.TryGetProperty("tag", out var t) ? t.GetString() ?? "" : "",
          e.TryGetProperty("reason", out var r) ? r.GetString() ?? "" : ""
        )
      );
    }

    return list;
  }

  /// <summary>Fixture path (<c>SET/CardName</c>, the shape the quarantine keys on) → the gold's
  /// <c>Input.Name</c>. The Parse track's own join, walked over the committed gold tree.</summary>
  public static IReadOnlyDictionary<string, string> LoadCardByFixture(string repoRoot)
  {
    var root = Path.Combine(repoRoot, HandParsedCardsRelPath);
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    if (!Directory.Exists(root))
      return map;

    foreach (var path in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories))
    {
      string? name;
      try
      {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        name = doc.RootElement.TryGetProperty("Input", out var input)
          && input.TryGetProperty("Name", out var n)
          ? n.GetString()
          : null;
      }
      catch (JsonException)
      {
        continue;
      }
      if (name is null)
        continue;

      var key = Path.GetRelativePath(root, path);
      key = key[..^".json".Length].Replace(Path.DirectorySeparatorChar, '/').Replace('\\', '/');
      map.TryAdd(key, name);
    }

    return map;
  }

  // ── Interaction track ────────────────────────────────────────────────────────────────────────────

  /// <summary>The shipped combo tier pins (Interaction track).</summary>
  public static IReadOnlyList<CrossTrackJoiner.ComboPin> LoadPins(string repoRoot)
  {
    var path = Path.Combine(repoRoot, ExpectedTiersRelPath);
    var list = new List<CrossTrackJoiner.ComboPin>();
    if (!File.Exists(path))
      return list;

    var root = JsonNode.Parse(File.ReadAllText(path));
    if (root?["combos"] is not JsonArray combos)
      return list;

    foreach (var combo in combos)
    {
      var id = combo?["id"]?.ToString();
      var tier = combo?["expectedTier"]?.ToString();
      if (id is null || tier is null)
        continue;
      var cards = (combo?["cards"] as JsonArray)?.Select(c => c?.ToString() ?? "").Where(s => s.Length > 0).ToList() ?? [];
      list.Add(new CrossTrackJoiner.ComboPin(id, tier, cards));
    }

    return list;
  }

  /// <summary>The named, human-reviewed <c>(comboId, fixture)</c> fidelity-risk carve-outs. Reused from
  /// the existing bench-ring gate (item R1) rather than duplicated — one acknowledgment list, two gates
  /// reading it. Liveness/shrink-only enforcement over that file stays with its owner,
  /// <c>MagicAtlas.Bench</c>'s <c>FidelityRiskGateTest</c>.</summary>
  public static IReadOnlySet<(string ComboId, string Fixture)> LoadAcknowledged(string repoRoot)
  {
    var path = Path.Combine(repoRoot, AcknowledgedRelPath);
    var set = new HashSet<(string, string)>();
    if (!File.Exists(path))
      return set;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (!doc.RootElement.TryGetProperty("entries", out var entries))
      return set;

    foreach (var e in entries.EnumerateArray())
    {
      if (!e.TryGetProperty("id", out var idEl) || idEl.GetString() is not { } id)
        continue;
      if (!e.TryGetProperty("fixtures", out var fixtures))
        continue;
      foreach (var f in fixtures.EnumerateArray())
        if (f.GetString() is { } fixture)
          set.Add((id, fixture));
    }

    return set;
  }

  /// <summary>Card name → the interaction gold ids naming it (context on join-1 rows).</summary>
  public static IReadOnlyDictionary<string, IReadOnlyList<string>> LoadInteractionGoldsByCard(string repoRoot)
  {
    var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
    foreach (var (id, gold) in EnumerateInteractionGolds(repoRoot))
      foreach (var cardNode in gold["cards"] as JsonArray ?? [])
        if (cardNode?.ToString() is { } card)
          (map.TryGetValue(card, out var list) ? list : map[card] = []).Add(id);

    return map.ToDictionary(
      kv => kv.Key,
      kv => (IReadOnlyList<string>)[.. kv.Value.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)],
      StringComparer.Ordinal
    );
  }

  // ── Join 2 sources: golds' declares, edge citations, the rollup, the engine sources ──────────────

  /// <summary>Every <c>declares[]</c> entry across the interaction golds — the ONLY source of the
  /// guard→witness map.</summary>
  public static (IReadOnlyList<CrossTrackJoiner.DeclaredRule> Declared, IReadOnlyList<CrossTrackJoiner.EdgeCitation> Citations, int GoldsScanned) LoadGoldDeclarations(
    string repoRoot
  )
  {
    var declared = new List<CrossTrackJoiner.DeclaredRule>();
    var citations = new List<CrossTrackJoiner.EdgeCitation>();
    var count = 0;

    foreach (var (id, gold) in EnumerateInteractionGolds(repoRoot))
    {
      count++;
      var judgePassed = gold["judge"]?["verdict"]?.ToString() == "PASS";

      if (gold["declares"] is JsonObject declares)
        foreach (var section in RuleSections)
        {
          if (declares[section] is not JsonArray arr)
            continue;
          foreach (var node in arr)
          {
            if (node is not JsonObject rule || rule["id"]?.ToString() is not { } ruleId)
              continue;
            declared.Add(
              new CrossTrackJoiner.DeclaredRule(
                GoldId: id,
                Section: section,
                RuleId: ruleId,
                Impl: rule["impl"]?.ToString(),
                Desc: rule["desc"]?.ToString(),
                Cr: (rule["cr"] as JsonArray)?.Select(c => c?.ToString() ?? "").Where(s => s.Length > 0).ToList() ?? [],
                JudgePassed: judgePassed
              )
            );
          }
        }

      foreach (var node in gold["edges"] as JsonArray ?? [])
      {
        if (node is not JsonObject edge || edge["rule"]?.ToString() is not { } cited)
          continue;
        citations.Add(
          new CrossTrackJoiner.EdgeCitation(id, edge["id"]?.ToString() ?? "?", cited, edge["tier"]?.ToString())
        );
      }
    }

    return (declared, citations, count);
  }

  /// <summary>The committed rollup's rules, read from the <c>.cited</c> twin (the only one carrying
  /// witness attribution). This is the middle leg: <c>declares → ROLLUP RULE → engine guard</c>.</summary>
  public static IReadOnlyList<CrossTrackJoiner.RollupRule> LoadRollupRules(string repoRoot)
  {
    var path = Path.Combine(repoRoot, RollupCitedRelPath);
    var list = new List<CrossTrackJoiner.RollupRule>();
    if (!File.Exists(path))
      return list;

    var root = JsonNode.Parse(File.ReadAllText(path));
    foreach (var section in RuleSections)
    {
      if (root?[section] is not JsonArray arr)
        continue;
      foreach (var node in arr)
      {
        if (node is not JsonObject rule || rule["id"]?.ToString() is not { } id)
          continue;
        list.Add(
          new CrossTrackJoiner.RollupRule(
            Section: section,
            RuleId: id,
            Status: rule["status"]?.ToString() ?? "",
            Witnesses: (rule["witnesses"] as JsonArray)?.Select(w => w?.ToString() ?? "").Where(s => s.Length > 0).ToList() ?? []
          )
        );
      }
    }

    return list;
  }

  /// <summary>
  /// Scans the engine sources for literal occurrences of each rule id — the <c>rollup rule → engine
  /// guard</c> leg, derived rather than declared. A guard whose id appears in no engine source cannot be
  /// bijected to its implementation; that gap is REPORTED here and closed by issue #34.
  /// </summary>
  /// <remarks>
  /// This flow's own directory and its gates are excluded from the scan: a rule id quoted in the joiner
  /// or in a gate message would otherwise register as its own implementation, which is exactly the
  /// self-verifying loop ADR-0004 §4 exists to break.
  /// </remarks>
  public static (IReadOnlyDictionary<string, IReadOnlyList<CrossTrackJoiner.CodeReference>> ByRuleId, int FilesScanned) ScanEngineSources(
    string repoRoot,
    IEnumerable<string> ruleIds
  )
  {
    var ids = ruleIds.Distinct(StringComparer.Ordinal).ToList();
    var hits = ids.ToDictionary(id => id, _ => new List<CrossTrackJoiner.CodeReference>(), StringComparer.Ordinal);
    var scanned = 0;

    var root = Path.Combine(repoRoot, EngineSourcesRelPath);
    if (!Directory.Exists(root) || ids.Count == 0)
      return (hits.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<CrossTrackJoiner.CodeReference>)kv.Value, StringComparer.Ordinal), 0);

    foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
    {
      var rel = Path.GetRelativePath(repoRoot, path).Replace(Path.DirectorySeparatorChar, '/');
      if (rel.Contains("/bin/", StringComparison.Ordinal) || rel.Contains("/obj/", StringComparison.Ordinal))
        continue;
      scanned++;

      var lines = File.ReadAllLines(path);
      for (var i = 0; i < lines.Length; i++)
        foreach (var id in ids)
          if (lines[i].Contains(id, StringComparison.Ordinal))
            hits[id].Add(new CrossTrackJoiner.CodeReference(rel, i + 1));
    }

    return (hits.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<CrossTrackJoiner.CodeReference>)kv.Value, StringComparer.Ordinal), scanned);
  }

  private static IEnumerable<(string Id, JsonObject Gold)> EnumerateInteractionGolds(string repoRoot)
  {
    var dir = Path.Combine(repoRoot, InteractionGoldsRelPath);
    if (!Directory.Exists(dir))
      yield break;

    foreach (var path in Directory.EnumerateFiles(dir, "*.json", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.Ordinal))
    {
      JsonNode? node;
      try
      {
        node = JsonNode.Parse(File.ReadAllText(path));
      }
      catch (JsonException)
      {
        continue;
      }
      if (node is JsonObject gold && gold["id"]?.ToString() is { } id)
        yield return (id, gold);
    }
  }
}
