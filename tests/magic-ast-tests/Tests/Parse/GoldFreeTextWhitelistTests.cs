namespace MagicAST.Tests.Tests;

using System.Text.Json;

/// <summary>
/// Free-text cleanliness invariant (de-string, initiative 05 — de-ratcheted 2026-06-16). A committed
/// gold under <c>Fixtures/HandParsedCards/**</c> must carry NO free-text residual sink — an
/// <c>[FreeTextField]</c> (<c>AbilityText</c>/<c>Instructions</c>/<c>Timeframe</c>) or an
/// <c>IResidual</c> "other" arm (<c>OtherCharacteristic</c>/<c>OtherCondition</c>/
/// <c>OtherHistoryPredicate</c>) — UNLESS the exact <c>(card, sink)</c> pair is named on
/// <c>Fixtures/whitelist-freetext.json</c>.
///
/// <para>
/// This replaces the opaque aggregate count baseline (<c>destring-sink-baseline.json</c>): the test
/// asserts an ABSOLUTE invariant, not a moving total. Failures are LOUD and per-card; new free-text debt
/// cannot hide behind a net-zero count; and every surviving exception is an EXPLICIT, justified carve-out
/// (each entry carries <c>tag</c> debt|irreducible + a <c>reason</c>). The whitelist is the only escape,
/// and it is a list of NAMES, never a number.
/// </para>
///
/// <para>
/// Two stateless checks: (1) every free-text instance in a gold must be whitelisted (else the gold is
/// loud-failing new debt); (2) every whitelist entry must still correspond to a live instance (a fixed
/// gold forces its entry's removal — the list only shrinks, but by being NAMED, not counted). It walks
/// the committed gold JSON directly (the artifact under contract), recursing every nested structure
/// (Saga chapters, Modal options, token sub-ASTs, granted abilities).
/// </para>
/// </summary>
[TestFixture]
public class GoldFreeTextWhitelistTests
{
  /// <summary>JSON keys carrying <c>[FreeTextField]</c> interior free text. Non-empty = one instance.</summary>
  private static readonly string[] FreeTextKeys = ["AbilityText", "Instructions", "Timeframe"];

  /// <summary><c>IResidual</c> residual-arm nodes, keyed by (discriminator JSON property, value) → sink name.</summary>
  private static readonly (string Prop, string Value, string Sink)[] ResidualArms =
  [
    ("CharacteristicType", "other", "OtherCharacteristic"),
    ("ConditionType", "other", "OtherCondition"),
    ("PredicateType", "other", "OtherHistoryPredicate"),
    // Fidelity-ladder L1 residual: a recognised ability shell whose effect interior
    // is held verbatim (the shell fallback). Detected by its EffectType discriminator
    // (its FreeTextField JSON key "Text" is too generic — Parenthetical reuses it),
    // so a gold carrying an L1 shell must name (card, "UnstructuredEffect") here —
    // accounted debt, burned down when the interior is structured (L1 → L2).
    ("EffectType", "unstructured", "UnstructuredEffect"),
  ];

  // card → the set of free-text sinks that card's gold carries (one walk over the corpus).
  private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlySet<string>>> _live = new(
    ScanLiveSinks
  );

  // The committed whitelist: the set of (card, sink) pairs permitted to carry free text.
  private static readonly Lazy<IReadOnlySet<(string Card, string Sink)>> _whitelist = new(
    LoadWhitelist
  );

  [Test]
  public void Corpus_and_whitelist_are_present()
  {
    Assert.That(GoldFiles(), Is.Not.Empty, "No gold fixtures under Fixtures/HandParsedCards/**.");
    // An empty whitelist is legitimate (the target end state) — only assert the file loads.
    Assert.That(_whitelist.Value, Is.Not.Null);
  }

  /// <summary>(1) Every free-text instance a gold carries must be an explicitly whitelisted (card, sink).</summary>
  [TestCaseSource(nameof(GoldCards))]
  public void Gold_carries_no_unwhitelisted_free_text(string card)
  {
    var sinks = _live.Value.GetValueOrDefault(card, new HashSet<string>());
    var unlisted = sinks.Where(s => !_whitelist.Value.Contains((card, s))).OrderBy(s => s).ToList();

    Assert.That(
      unlisted,
      Is.Empty,
      $"Gold '{card}' carries free-text sink(s) [{string.Join(", ", unlisted)}] not on "
        + "Fixtures/whitelist-freetext.json. Gold must not assert free text as truth — de-string it "
        + "(structure the residual), or, only if genuinely irreducible, add the (card, sink) to the "
        + "whitelist with a tag+reason. New debt is rejected outright; the whitelist holds only named, "
        + "justified carve-outs."
    );
  }

  /// <summary>(2) Every whitelist entry must still correspond to a live free-text instance (else remove it).</summary>
  [TestCaseSource(nameof(WhitelistPairs))]
  public void Whitelist_entry_still_carries_its_sink(string card, string sink)
  {
    var sinks = _live.Value.GetValueOrDefault(card, new HashSet<string>());
    Assert.That(
      sinks,
      Does.Contain(sink),
      $"'{card}' is whitelisted for free-text sink '{sink}' but no longer carries it — the gold was "
        + "de-stringed. Remove the entry from Fixtures/whitelist-freetext.json. The whitelist holds only "
        + "live, named carve-outs (it only shrinks)."
    );
  }

  public static IEnumerable<string> GoldCards() =>
    _live.Value.Keys.OrderBy(c => c, StringComparer.Ordinal);

  public static IEnumerable<TestCaseData> WhitelistPairs() =>
    _whitelist
      .Value.OrderBy(p => p.Card, StringComparer.Ordinal)
      .ThenBy(p => p.Sink, StringComparer.Ordinal)
      .Select(p => new TestCaseData(p.Card, p.Sink));

  private static IReadOnlyDictionary<string, IReadOnlySet<string>> ScanLiveSinks()
  {
    var root = Path.Combine(GoldRoot(), "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");
    var result = new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal);

    foreach (var file in GoldFiles())
    {
      var card = Path
        .GetRelativePath(root, file)
        .Replace('\\', '/')[..^".json".Length];

      using var doc = JsonDocument.Parse(File.ReadAllText(file));
      var sinks = new HashSet<string>(StringComparer.Ordinal);
      if (doc.RootElement.TryGetProperty("Output", out var output))
      {
        CollectSinks(output, sinks);
      }

      if (sinks.Count > 0)
      {
        result[card] = sinks;
      }
    }

    return result;
  }

  private static void CollectSinks(JsonElement node, HashSet<string> sinks)
  {
    switch (node.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (var key in FreeTextKeys)
        {
          if (node.TryGetProperty(key, out var v) && IsNonEmptyFreeText(v))
          {
            sinks.Add(key);
          }
        }

        foreach (var (prop, value, sink) in ResidualArms)
        {
          if (
            node.TryGetProperty(prop, out var disc)
            && disc.ValueKind == JsonValueKind.String
            && disc.GetString() == value
          )
          {
            sinks.Add(sink);
          }
        }

        foreach (var member in node.EnumerateObject())
        {
          CollectSinks(member.Value, sinks);
        }

        break;

      case JsonValueKind.Array:
        foreach (var item in node.EnumerateArray())
        {
          CollectSinks(item, sinks);
        }

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

  private static IReadOnlySet<(string Card, string Sink)> LoadWhitelist()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "whitelist-freetext.json"
    );
    var set = new HashSet<(string, string)>();
    if (!File.Exists(path))
    {
      return set;
    }

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
    {
      foreach (var e in entries.EnumerateArray())
      {
        var card = e.TryGetProperty("card", out var c) ? c.GetString() : null;
        var sink = e.TryGetProperty("sink", out var s) ? s.GetString() : null;
        if (card is not null && sink is not null)
        {
          set.Add((card, sink));
        }
      }
    }

    return set;
  }

  private static IReadOnlyList<string> GoldFiles()
  {
    var dir = Path.Combine(GoldRoot(), "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");
    return Directory.Exists(dir)
      ? Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories).ToList()
      : [];
  }

  // The committed gold JSON is the artifact under contract — walk the source tree, not the output
  // mirror. Locate the repo root by the nx.json sentinel (same pattern as SchemaExportTests).
  private static string GoldRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
    {
      dir = dir.Parent;
    }

    return dir?.FullName
      ?? throw new InvalidOperationException(
        "Could not locate repo root (no nx.json above the test directory)."
      );
  }
}
