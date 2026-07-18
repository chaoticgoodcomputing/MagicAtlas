using System.Text.Json.Nodes;

namespace MagicAtlas.Bench;

/// <summary>One entry on <c>tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json</c>, keyed by
/// the fixture's RELATIVE PATH (e.g. <c>"NPH/SuturePriest"</c>) — NOT the card's display name.</summary>
public sealed record QuarantineEntry(string Fixture, string Tag, string Reason);

/// <summary>
/// The oracle-text drift/quarantine whitelist, loaded from the SAME source-of-truth file the MAST
/// <c>GoldOracleTextFidelityTests</c> gate reads (linked into the bench build output —
/// <see cref="BenchPaths.QuarantinePath"/>). Item R1: joins against <see cref="GoldCorpus.FixturePathFor"/>
/// (<c>Input.Name</c> → fixture path) so <c>ComboRecallRunner</c> can flag a combo whose Output AST was
/// derived from a fixture already known to drift from its authoritative oracle text — exactly the
/// Suture Priest incident this session, now caught mechanically instead of by a judge digging by hand.
/// </summary>
public sealed class QuarantineIndex
{
  private readonly IReadOnlyDictionary<string, QuarantineEntry> _byFixturePath;

  private QuarantineIndex(IReadOnlyDictionary<string, QuarantineEntry> byFixturePath) =>
    _byFixturePath = byFixturePath;

  /// <summary>An empty index — the safe default when the quarantine file is absent (never fabricate risk).</summary>
  public static QuarantineIndex Empty { get; } =
    new(new Dictionary<string, QuarantineEntry>(StringComparer.Ordinal));

  public bool TryGet(string fixturePath, out QuarantineEntry entry) =>
    _byFixturePath.TryGetValue(fixturePath, out entry!);

  /// <summary>Loads <c>{ "entries": [ { "card": "<fixturePath>", "tag": ..., "reason": ... } ] }</c>.
  /// Returns <see cref="Empty"/> (not a throw) when the file is missing — a copy-to-output ordering
  /// hiccup should degrade to "no known risk", never crash the bench.</summary>
  public static QuarantineIndex Load(string path)
  {
    if (!File.Exists(path))
      return Empty;

    var map = new Dictionary<string, QuarantineEntry>(StringComparer.Ordinal);
    var root = JsonNode.Parse(File.ReadAllText(path));
    if (root?["entries"] is JsonArray entries)
    {
      foreach (var e in entries)
      {
        var fixture = e?["card"]?.GetValue<string>();
        if (string.IsNullOrEmpty(fixture))
          continue;
        var tag = e?["tag"]?.GetValue<string>() ?? "";
        var reason = e?["reason"]?.GetValue<string>() ?? "";
        map[fixture] = new QuarantineEntry(fixture, tag, reason);
      }
    }

    return new QuarantineIndex(map);
  }
}
