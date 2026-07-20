using System.Text.Json;

namespace MagicAtlas.Ast.Tests.Flows.Common;

/// <summary>
/// The single definition of what a <b>free-text residual sink</b> is, and the walk that finds them in a
/// committed gold. Shared by the CORE-ring gate (<c>GoldFreeTextWhitelistTests</c>, which asserts the
/// stateless named-(card, sink) whitelist invariant) and the <c>FreeTextResidualCensus</c> Flowthru flow
/// (which reports the initiative-05 burn-down numbers).
///
/// <para>Deliberately NOT a Flowthru step — a step would bind the gate to a generated artifact, and the
/// gate must hold on a clean checkout with no <c>_08_Reporting</c> present. Same pattern as
/// <c>ArtifactClassifier</c>.</para>
///
/// <para><b>Why this class exists.</b> <c>libs/magic-ast/schema/destring-worklist.json</c> used to hold
/// these numbers as a hand-committed, deliberately frozen measurement, with the sink definitions
/// restated in its <c>_method</c> prose. That is the shape ADR-0004 retired in
/// <c>discriminator-baseline.json</c>: a measurement about the repo, frozen, with no regenerator, so it
/// could only drift away from the golds it described. The measurement is now recomputed on demand and
/// the definition lives in one place that both the gate and the report read.</para>
/// </summary>
public static class FreeTextSinkScanner
{
  /// <summary>JSON keys carrying <c>[FreeTextField]</c> interior free text. Non-empty = one instance.</summary>
  public static readonly string[] FreeTextKeys = ["AbilityText", "Instructions", "Timeframe"];

  /// <summary><c>IResidual</c> residual-arm nodes, keyed by (discriminator JSON property, value) → sink.</summary>
  public static readonly (string Prop, string Value, string Sink)[] ResidualArms =
  [
    ("CharacteristicType", "other", "OtherCharacteristic"),
    ("ConditionType", "other", "OtherCondition"),
    ("PredicateType", "other", "OtherHistoryPredicate"),
    // Fidelity-ladder L1 residual: a recognised ability shell whose effect interior is held verbatim.
    // Detected by its EffectType discriminator (its FreeTextField JSON key "Text" is too generic —
    // Parenthetical reuses it).
    ("EffectType", "unstructured", "UnstructuredEffect"),
  ];

  /// <summary>Every sink name, in report order.</summary>
  public static IReadOnlyList<string> AllSinks =>
    FreeTextKeys.Concat(ResidualArms.Select(a => a.Sink)).ToList();

  /// <summary>One scanned gold: its whitelist-key name plus per-sink instance counts.</summary>
  public sealed record GoldScan(string Card, IReadOnlyDictionary<string, int> Instances)
  {
    /// <summary>The sinks this gold carries (the gate's view — presence, not count).</summary>
    public IReadOnlySet<string> Sinks =>
      Instances.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToHashSet(StringComparer.Ordinal);
  }

  /// <summary>The gold root under a repo root.</summary>
  public static string GoldsRoot(string repoRoot) =>
    Path.Combine(repoRoot, "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");

  public static IReadOnlyList<string> GoldFiles(string repoRoot)
  {
    var dir = GoldsRoot(repoRoot);
    return Directory.Exists(dir)
      ? Directory
        .EnumerateFiles(dir, "*.json", SearchOption.AllDirectories)
        .OrderBy(f => f, StringComparer.Ordinal)
        .ToList()
      : [];
  }

  /// <summary>Scan every committed gold. The card key is the gold's path relative to
  /// <c>HandParsedCards/</c> without <c>.json</c> (forward-slashed) — the same key the whitelist and the
  /// NUnit test-case names use.</summary>
  public static IReadOnlyList<GoldScan> ScanAll(string repoRoot)
  {
    var root = GoldsRoot(repoRoot);
    var results = new List<GoldScan>();
    foreach (var file in GoldFiles(repoRoot))
    {
      var card = Path.GetRelativePath(root, file).Replace('\\', '/')[..^".json".Length];
      var counts = new Dictionary<string, int>(StringComparer.Ordinal);
      JsonDocument doc;
      try
      {
        doc = JsonDocument.Parse(File.ReadAllText(file));
      }
      catch (JsonException)
      {
        continue;
      }
      using (doc)
      {
        if (doc.RootElement.TryGetProperty("Output", out var output))
          Collect(output, counts);
      }
      results.Add(new GoldScan(card, counts));
    }
    return results;
  }

  private static void Collect(JsonElement node, Dictionary<string, int> counts)
  {
    switch (node.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (var key in FreeTextKeys)
          if (node.TryGetProperty(key, out var v) && IsNonEmptyFreeText(v))
            counts[key] = counts.GetValueOrDefault(key) + 1;

        foreach (var (prop, value, sink) in ResidualArms)
          if (
            node.TryGetProperty(prop, out var disc)
            && disc.ValueKind == JsonValueKind.String
            && disc.GetString() == value
          )
            counts[sink] = counts.GetValueOrDefault(sink) + 1;

        foreach (var member in node.EnumerateObject())
          Collect(member.Value, counts);
        break;

      case JsonValueKind.Array:
        foreach (var item in node.EnumerateArray())
          Collect(item, counts);
        break;
    }
  }

  private static bool IsNonEmptyFreeText(JsonElement v) =>
    v.ValueKind switch
    {
      JsonValueKind.String => !string.IsNullOrWhiteSpace(v.GetString()),
      JsonValueKind.Array => v.GetArrayLength() > 0,
      _ => false,
    };

  /// <summary>The committed named carve-outs: (card, sink) → tag. Read from
  /// <c>Fixtures/whitelist-freetext.json</c>.</summary>
  public static IReadOnlyDictionary<(string Card, string Sink), string> LoadWhitelist(string repoRoot)
  {
    var path = Path.Combine(
      repoRoot, "tests", "magic-ast-tests", "Fixtures", "whitelist-freetext.json"
    );
    var map = new Dictionary<(string, string), string>();
    if (!File.Exists(path))
      return map;
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (!doc.RootElement.TryGetProperty("entries", out var entries))
      return map;
    foreach (var e in entries.EnumerateArray())
    {
      var card = e.TryGetProperty("card", out var c) ? c.GetString() : null;
      var sink = e.TryGetProperty("sink", out var s) ? s.GetString() : null;
      var tag = e.TryGetProperty("tag", out var t) ? t.GetString() : null;
      if (card is not null && sink is not null)
        map[(card, sink)] = tag ?? "untagged";
    }
    return map;
  }
}
