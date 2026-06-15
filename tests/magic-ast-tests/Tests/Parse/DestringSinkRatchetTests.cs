namespace MagicAST.Tests.Tests;

using System.Text.Json;

/// <summary>
/// Initiative 05 (de-string the AST leaves) free-text-sink ratchet. Walks every
/// committed gold under <c>Fixtures/HandParsedCards/**</c> and tallies, per known
/// free-text sink, how many INSTANCES the corpus still carries. The committed
/// baseline (<c>Fixtures/destring-sink-baseline.json</c>) may only SHRINK: a sink
/// whose live count drops below baseline fails loudly and forces a baseline
/// update; a sink that grows is rejected outright. Same shrink-only contract as
/// <c>Fixtures/oracle-text-quarantine.json</c> and <c>KnownUnparsedGold</c>.
///
/// <para>
/// This is the measurement that lands FIRST — it freezes the migration debt
/// before the de-string refactor starts, so every batch that lands afterward can
/// only reduce it. It deliberately walks the committed gold JSON directly
/// (<see cref="JsonDocument"/>), recursing into every nested structure (Saga
/// chapters, Modal options, token sub-ASTs, granted abilities), rather than the
/// in-memory AST — the gold files are the artifact under contract.
/// </para>
///
/// <para>
/// The sinks are the AST's own debt markers (<c>libs/magic-ast/AST/Residual.cs</c>):
/// the <c>[FreeTextField]</c> properties — serialized JSON keys <c>AbilityText</c>
/// (<c>TokenDefinition</c>, <c>AbilityAdder</c>, <c>LoseAbilityEffect</c>),
/// <c>Instructions</c> (<c>SpellAbility</c>, <c>TriggeredAbility</c>),
/// <c>Timeframe</c> (<c>HistoryPredicate</c>) — plus the <c>IResidual</c> residual
/// arms detected by their polymorphic discriminator: <c>OtherCharacteristic</c>
/// (<c>CharacteristicType=other</c>), <c>OtherCondition</c>
/// (<c>ConditionType=other</c>), <c>OtherHistoryPredicate</c>
/// (<c>PredicateType=other</c>). The affected cards are enumerated in the
/// companion burn-down worklist at
/// <c>libs/magic-ast/schema/destring-worklist.json</c>.
/// </para>
/// </summary>
[TestFixture]
public class DestringSinkRatchetTests
{
  /// <summary>JSON keys carrying <c>[FreeTextField]</c> interior free text. Non-empty = one instance.</summary>
  private static readonly string[] FreeTextKeys = ["AbilityText", "Instructions", "Timeframe"];

  /// <summary>
  /// <c>IResidual</c> residual-arm nodes, keyed by (discriminator JSON property, value).
  /// The dictionary value is the sink name reported in the baseline.
  /// </summary>
  private static readonly (string Prop, string Value, string Sink)[] ResidualArms =
  [
    ("CharacteristicType", "other", "OtherCharacteristic"),
    ("ConditionType", "other", "OtherCondition"),
    ("PredicateType", "other", "OtherHistoryPredicate"),
  ];

  private static readonly Lazy<IReadOnlyDictionary<string, int>> _baseline = new(LoadBaseline);
  private static readonly Lazy<IReadOnlyDictionary<string, int>> _live = new(CountLiveSinks);

  /// <summary>Every sink in the baseline is a test case, so each gets its own pass/fail line.</summary>
  public static IEnumerable<string> SinkNames() => _baseline.Value.Keys.OrderBy(k => k, StringComparer.Ordinal);

  [Test]
  public void Baseline_and_corpus_are_present()
  {
    Assert.That(
      _baseline.Value,
      Is.Not.Empty,
      "destring-sink-baseline.json is missing or empty — the ratchet cannot freeze debt without it."
    );
    Assert.That(
      GoldFiles(),
      Is.Not.Empty,
      "No gold fixtures found under Fixtures/HandParsedCards/** — cannot measure free-text sinks."
    );
  }

  [TestCaseSource(nameof(SinkNames))]
  public void Sink_count_only_shrinks(string sink)
  {
    var baseline = _baseline.Value[sink];
    var live = _live.Value.GetValueOrDefault(sink);

    Assert.That(
      live,
      Is.LessThanOrEqualTo(baseline),
      $"Free-text sink '{sink}' GREW: {live} instances now vs baseline {baseline}. "
        + "New free-text debt may not land (initiative 05 freezes the leaves). De-string the new "
        + "occurrence(s) or, if genuinely a typed residual, justify and update the baseline deliberately."
    );

    Assert.That(
      live,
      Is.EqualTo(baseline),
      $"Free-text sink '{sink}' SHRANK: {live} instances now vs baseline {baseline} — progress! "
        + "Lower the baseline in Fixtures/destring-sink-baseline.json to lock the win (the ratchet only shrinks)."
    );
  }

  private static IReadOnlyDictionary<string, int> CountLiveSinks()
  {
    var counts = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var key in FreeTextKeys)
    {
      counts[key] = 0;
    }
    foreach (var (_, _, sink) in ResidualArms)
    {
      counts[sink] = 0;
    }

    foreach (var file in GoldFiles())
    {
      using var doc = JsonDocument.Parse(File.ReadAllText(file));
      if (doc.RootElement.TryGetProperty("Output", out var output))
      {
        Walk(output, counts);
      }
    }

    return counts;
  }

  private static void Walk(JsonElement node, Dictionary<string, int> counts)
  {
    switch (node.ValueKind)
    {
      case JsonValueKind.Object:
        foreach (var key in FreeTextKeys)
        {
          if (node.TryGetProperty(key, out var v) && IsNonEmptyFreeText(v))
          {
            counts[key]++;
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
            counts[sink]++;
          }
        }

        foreach (var member in node.EnumerateObject())
        {
          Walk(member.Value, counts);
        }

        break;

      case JsonValueKind.Array:
        foreach (var item in node.EnumerateArray())
        {
          Walk(item, counts);
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

  private static IReadOnlyDictionary<string, int> LoadBaseline()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "destring-sink-baseline.json"
    );
    if (!File.Exists(path))
    {
      return new Dictionary<string, int>(StringComparer.Ordinal);
    }

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    var result = new Dictionary<string, int>(StringComparer.Ordinal);
    if (doc.RootElement.TryGetProperty("sinks", out var sinks))
    {
      foreach (var member in sinks.EnumerateObject())
      {
        result[member.Name] = member.Value.GetInt32();
      }
    }

    return result;
  }

  private static IReadOnlyList<string> GoldFiles()
  {
    var dir = Path.Combine(GoldRoot(), "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");
    return Directory.Exists(dir)
      ? Directory.EnumerateFiles(dir, "*.json", SearchOption.AllDirectories).ToList()
      : [];
  }

  // The committed gold JSON is the artifact under contract, so walk the source
  // tree, not the copy-to-output mirror. Locate the repo root by the nx.json
  // sentinel (same pattern as SchemaExportTests).
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
