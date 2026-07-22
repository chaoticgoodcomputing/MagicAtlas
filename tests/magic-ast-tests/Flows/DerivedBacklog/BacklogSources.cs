namespace MagicAtlas.Ast.Tests.Flows.DerivedBacklog;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// The I/O half of the backlog derivation (kept apart from <see cref="BacklogDerivation.Compute"/> so the
/// formula stays hermetically self-testable). Every source here is a committed artifact — the interaction
/// golds, the reconstruction pins — so the discriminator backlog and its gate run identically on a clean
/// checkout with no corpus.
/// </summary>
public static partial class BacklogSources
{
  public const string InteractionGoldsRelPath = "tests/magic-ast-tests/Fixtures/Interactions/golds";
  public const string ComboPinsRelPath = "tools/bench/MagicAtlas.Bench/combo-axis-expectations.json";

  /// <summary>
  /// The <c>owner</c> attribute axis is the one declared axis ADR-0003 §4a.1 routes to the derived backlog:
  /// "an unwitnessed prediction is not a ruling — it is backlog … where it reappears the moment a gold's
  /// port carries an <c>owner</c> attribute." It is an architectural expectation (the scaffold's
  /// <c>attribute_axes</c> was dissolved by #26/§7), not drift-prone state — its liveness is proven by the
  /// witnessed-subtraction below, which drops it the instant a gold witnesses it.
  /// </summary>
  public static readonly IReadOnlyList<string> DeclaredUnwitnessedAxes = ["owner"];

  private static readonly string[] AxisMetaKeys = ["provenance", "polarity"];

  [GeneratedRegex(@"^no_arm\[(?<port>[^\]]+)\]$")]
  private static partial Regex NoArmClaim();

  public static string RepoRoot(string? from = null)
  {
    var dir = new DirectoryInfo(from ?? AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException(
        "Could not locate the workspace root (no nx.json above " + (from ?? AppContext.BaseDirectory) + ")."
      );
  }

  public static string GoldsDir(string repoRoot) => Path.Combine(repoRoot, InteractionGoldsRelPath);

  /// <summary>One resolved asserted-unarmable decision: the discriminator it removes, and the gold + claim
  /// that removes it.</summary>
  public sealed record DecisionSource(BacklogDerivation.Term Term, string Gold, string Claim);

  /// <summary>
  /// The subtrahend, derived LIVE from the golds. For every <c>no_arm[Pn]</c> assertion, resolve port
  /// <c>Pn</c> and map its live coarse label (and its canonical stem) to a discriminator, case-insensitively
  /// (the engine lowercases a coarse label — <c>emit:anynumberindeck</c> — while the discriminator is
  /// <c>anyNumberInDeck</c>). A gold that arms nothing subtracts nothing; an empty golds set yields an empty
  /// subtrahend, which is the normal case.
  /// </summary>
  public static IReadOnlyList<DecisionSource> LoadAssertedUnarmable(
    string goldsDir,
    IReadOnlyDictionary<string, IReadOnlySet<string>> allByDim
  )
  {
    // discriminator (lowercased) -> (dimension, real spelling); first-writer wins across dims (names are
    // effectively unique per token in practice).
    var byLower = new Dictionary<string, BacklogDerivation.Term>(StringComparer.Ordinal);
    foreach (var (dim, all) in allByDim)
      foreach (var d in all)
        byLower.TryAdd(d.ToLowerInvariant(), new BacklogDerivation.Term(dim, d));

    var results = new List<DecisionSource>();
    if (!Directory.Exists(goldsDir))
      return results;

    foreach (var path in Directory.EnumerateFiles(goldsDir, "*.json", SearchOption.TopDirectoryOnly)
      .OrderBy(p => p, StringComparer.Ordinal))
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
      if (node is not JsonObject gold || gold["id"]?.ToString() is not { } goldId)
        continue;

      // port id -> the spellings to probe (coarse label minus side prefix, and the canonical stem).
      var probesByPort = new Dictionary<string, List<string>>(StringComparer.Ordinal);
      if (gold["ports"] is JsonObject portsByCard)
        foreach (var card in portsByCard)
          foreach (var p in card.Value as JsonArray ?? [])
          {
            if (p is not JsonObject port || port["id"]?.ToString() is not { } pid)
              continue;
            var probes = new List<string>();
            if (port["coarse_label"]?.ToString() is { } coarse)
              probes.Add(StripSide(coarse));
            if (port["stem"]?.ToString() is { } stem)
              probes.Add(stem);
            probesByPort[pid] = probes;
          }

      foreach (var a in gold["assertions"] as JsonArray ?? [])
      {
        if (a is not JsonObject assertion || assertion["claim"]?.ToString() is not { } claim)
          continue;
        var m = NoArmClaim().Match(claim);
        if (!m.Success)
          continue;
        var portId = m.Groups["port"].Value;
        if (!probesByPort.TryGetValue(portId, out var probes))
          continue;
        foreach (var probe in probes)
          if (byLower.TryGetValue(probe.ToLowerInvariant(), out var term)
            && !results.Any(r => r.Term == term && r.Gold == goldId))
            results.Add(new DecisionSource(term, goldId, claim));
      }
    }

    return results
      .OrderBy(r => r.Term.Dimension, StringComparer.Ordinal)
      .ThenBy(r => r.Term.Discriminator, StringComparer.Ordinal)
      .ToList();
  }

  /// <summary>The coarse label minus its side prefix (<c>emit:anynumberindeck</c> → <c>anynumberindeck</c>);
  /// a label with no recognized prefix is returned verbatim.</summary>
  private static string StripSide(string label)
  {
    var colon = label.IndexOf(':');
    if (colon < 0)
      return label;
    var side = label[..colon];
    return side is "emit" or "consume" or "pay" ? label[(colon + 1)..] : label;
  }

  /// <summary>The attribute axes any gold's port witnesses — the union of every port's <c>attrs</c> keys,
  /// derived live. Meta keys (<c>provenance</c>/<c>polarity</c>) are not axes.</summary>
  public static IReadOnlyList<string> WitnessedAttributeAxes(string goldsDir)
  {
    var axes = new SortedSet<string>(StringComparer.Ordinal);
    if (!Directory.Exists(goldsDir))
      return axes.ToList();

    foreach (var path in Directory.EnumerateFiles(goldsDir, "*.json", SearchOption.TopDirectoryOnly))
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
      if (node is not JsonObject gold || gold["ports"] is not JsonObject portsByCard)
        continue;
      foreach (var card in portsByCard)
        foreach (var p in card.Value as JsonArray ?? [])
          if (p is JsonObject port && port["attrs"] is JsonObject attrs)
            foreach (var kv in attrs)
              if (!AxisMetaKeys.Contains(kv.Key))
                axes.Add(kv.Key);
    }

    return axes.ToList();
  }

  /// <summary>
  /// Combo-level unserved demand: the reconstruction pins' <c>unreconstructed</c> section — combos over
  /// whose gold ASTs the engine reconstructs no spanning cycle (#31a moved four here as "#32's
  /// inheritance"). A different GRANULARITY from the discriminator backlog, surfaced in the same report so
  /// unserved demand has one home. Read-only over the committed bench file; graceful degrade when absent.
  /// </summary>
  public static (bool Available, IReadOnlyList<UnreconstructedCombo> Combos) LoadUnreconstructedCombos(string repoRoot)
  {
    var path = Path.Combine(repoRoot, ComboPinsRelPath);
    if (!File.Exists(path))
      return (false, []);

    JsonNode? root;
    try
    {
      root = JsonNode.Parse(File.ReadAllText(path));
    }
    catch (JsonException)
    {
      return (false, []);
    }

    var combos = new List<UnreconstructedCombo>();
    foreach (var u in root?["unreconstructed"] as JsonArray ?? [])
    {
      if (u is not JsonObject o || o["combo"]?.ToString() is not { } id)
        continue;
      combos.Add(new UnreconstructedCombo
      {
        Combo = id,
        Verdict = o["verdict"]?.ToString() ?? "no-reconstruction",
        Note = o["note"]?.ToString() ?? "",
      });
    }

    return (true, combos.OrderBy(c => c.Combo, StringComparer.Ordinal).ToList());
  }
}
