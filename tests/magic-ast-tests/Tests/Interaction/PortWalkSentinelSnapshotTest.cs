namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Alignment initiative 03, blocking criterion #3 — the end-to-end <b>sentinel snapshot</b> over the
/// FULL interaction pipeline (<see cref="PortWalk"/> → <see cref="PortGraphEngine"/>). A committed,
/// canonically-serialized snapshot of every currently-verified GREEN/AMBER combo plus a family-covering
/// spread of single-card sentinels (≥50 cards, every ability Kind + every PortWalk-projected
/// discriminator). Any pipeline change that alters an output fails the test until the snapshot is
/// regenerated (via the [Explicit] regen) and the diff is justified in the commit message. This is the
/// only test that catches a <em>cross-pillar</em> regression — a parser node-shape change silently
/// dropping a port — that the targeted per-feature tests cannot see.
///
/// <para>This test is purely additive: it never touches the pipeline. The serializer is canonical and
/// deterministic (collections sorted by a stable key, enums as strings, indented) so the snapshot is
/// not flaky and a second regen is byte-identical to the first.</para>
/// </summary>
[TestFixture]
public class PortWalkSentinelSnapshotTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  // --- locations (committed in the SOURCE tree, like SchemaExportTests — the snapshots are an artifact
  // we diff against, so they're read from the repo, not the bin/ output dir) ---

  private static string SnapshotsDir() =>
    Path.Combine(
      RepoRoot(),
      "tests",
      "magic-ast-tests",
      "Tests",
      "Interaction",
      "Snapshots"
    );

  private static string FixturesDir() =>
    Path.Combine(RepoRoot(), "tests", "magic-ast-tests", "Fixtures");

  private static string ManifestPath() => Path.Combine(SnapshotsDir(), "sentinels.json");

  // --- the manifest model ---

  public sealed record SentinelCardRef
  {
    public required string Path { get; init; }
    public required string Card { get; init; }
  }

  public sealed record Sentinel
  {
    public required string Name { get; init; }
    public required string Kind { get; init; } // "card" | "combo"
    public required IReadOnlyList<SentinelCardRef> Cards { get; init; }

    public override string ToString() => $"{Kind}:{Name}";
  }

  private static IReadOnlyList<Sentinel> LoadManifest()
  {
    var root = JsonNode.Parse(File.ReadAllText(ManifestPath()))!;
    var entries = root["entries"]!.AsArray();
    var list = new List<Sentinel>();
    foreach (var e in entries)
    {
      var cards = e!["cards"]!
        .AsArray()
        .Select(c => new SentinelCardRef
        {
          Path = c!["path"]!.ToString(),
          Card = c!["card"]!.ToString(),
        })
        .ToList();
      list.Add(
        new Sentinel
        {
          Name = e!["name"]!.ToString(),
          Kind = e!["kind"]!.ToString(),
          Cards = cards,
        }
      );
    }
    return list;
  }

  public static IEnumerable<TestCaseData> Sentinels() =>
    LoadManifest().Select(s => new TestCaseData(s).SetName($"Snapshot_{Slug(s.Name)}"));

  // --- the pipeline run + canonical projection ---

  /// <summary>The full pipeline for one sentinel: walk each card to a <see cref="PortGraph"/>, then —
  /// for a combo — materialize the inter-card edges and reconstruct the cycles. The single-card case is
  /// a degenerate combo (one graph): it still materializes its card-defined + intra-card derived edges
  /// and any self-loops, so a single card's snapshot is the same canonical shape as a combo's.</summary>
  private static SentinelOutput Run(Sentinel sentinel)
  {
    var walk = new PortWalk(Ontology);
    var graphs = sentinel
      .Cards.Select(c =>
      {
        var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(FixturesDir(), c.Path)));
        var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
          ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
          ?["Symbols"];
        return walk.Project(c.Card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost);
      })
      .ToList();

    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(graphs);
    var cycles = engine.FindCycles(edges);

    return new SentinelOutput
    {
      Name = sentinel.Name,
      Graphs = graphs,
      Edges = edges,
      Cycles = cycles,
    };
  }

  private sealed record SentinelOutput
  {
    public required string Name { get; init; }
    public required IReadOnlyList<PortGraph> Graphs { get; init; }
    public required IReadOnlyList<PortEdge> Edges { get; init; }
    public required IReadOnlyList<PortCycle> Cycles { get; init; }
  }

  // --- the CANONICAL, DETERMINISTIC serializer ---
  //
  // Everything is projected into stable DTOs whose collections are pre-sorted by a deterministic key:
  //   ports  : ordered by (Identity, Label)
  //   edges  : ordered by their full string form
  //   cycles : ordered by a stable signature (the sorted edge-strings joined), then by tier
  // Enums serialize as their string names; output is indented. Serializing the same SentinelOutput
  // twice is byte-identical (the round-trip stability the snapshot relies on).

  private static readonly JsonSerializerOptions CanonicalOptions =
    new()
    {
      WriteIndented = true,
      DefaultIgnoreCondition = JsonIgnoreCondition.Never,
      Converters = { new JsonStringEnumConverter() },
    };

  private static string Canonical(SentinelOutput output)
  {
    var ports = output
      .Graphs.SelectMany(g => g.Ports)
      .Select(PortDto.Of)
      .OrderBy(p => p.Identity, StringComparer.Ordinal)
      .ThenBy(p => p.Label, StringComparer.Ordinal)
      .ToList();

    var cardDefined = output
      .Graphs.SelectMany(g => g.CardDefinedEdges)
      .Select(CardEdgeDto.Of)
      .OrderBy(e => e.Signature, StringComparer.Ordinal)
      .ToList();

    var edges = output
      .Edges.Select(EdgeDto.Of)
      .OrderBy(e => e.Signature, StringComparer.Ordinal)
      .ToList();

    var cycles = output
      .Cycles.Select(CycleDto.Of)
      .OrderBy(c => c.Signature, StringComparer.Ordinal)
      .ThenBy(c => c.Tier, StringComparer.Ordinal)
      .ToList();

    var dto = new SnapshotDto
    {
      Name = output.Name,
      Ports = ports,
      CardDefinedEdges = cardDefined,
      DerivedEdges = edges,
      Cycles = cycles,
    };
    return JsonSerializer.Serialize(dto, CanonicalOptions);
  }

  // --- stable DTOs (the only thing the snapshot persists; insulated from the record's field order) ---

  private sealed record SnapshotDto
  {
    public required string Name { get; init; }
    public required IReadOnlyList<PortDto> Ports { get; init; }
    public required IReadOnlyList<CardEdgeDto> CardDefinedEdges { get; init; }
    public required IReadOnlyList<EdgeDto> DerivedEdges { get; init; }
    public required IReadOnlyList<CycleDto> Cycles { get; init; }
  }

  private sealed record PortDto
  {
    public required string Identity { get; init; }
    public required string Card { get; init; }
    public required string Label { get; init; }
    public required string Side { get; init; }
    public int? Quantity { get; init; }
    public bool Gated { get; init; }
    public bool TapGated { get; init; }
    public string? RequiresCounter { get; init; }
    public string? Subject { get; init; }

    public static PortDto Of(PortNode p) =>
      new()
      {
        Identity = p.Identity,
        Card = p.Card,
        Label = p.Label,
        Side = p.Side.ToString(),
        Quantity = p.Quantity,
        Gated = p.Gated,
        TapGated = p.TapGated,
        RequiresCounter = p.RequiresCounter,
        Subject = SubjectSignature(p.Subject),
      };
  }

  private sealed record CardEdgeDto
  {
    public required string From { get; init; }
    public required string To { get; init; }

    [JsonIgnore]
    public string Signature => $"{From}=>{To}";

    public static CardEdgeDto Of(CardDefinedEdge e) =>
      new() { From = e.From.Identity, To = e.To.Identity };
  }

  private sealed record EdgeDto
  {
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Provenance { get; init; }
    public required string Family { get; init; }
    public required string Overlap { get; init; }
    public required string Reliability { get; init; }
    public required string Tier { get; init; }
    public string? Reason { get; init; }

    [JsonIgnore]
    public string Signature =>
      $"{From}=>{To}|{Provenance}|{Family}|{Overlap}|{Reliability}|{Tier}|{Reason}";

    public static EdgeDto Of(PortEdge e) =>
      new()
      {
        From = e.From.Identity,
        To = e.To.Identity,
        Provenance = e.Provenance.ToString(),
        Family = e.Family.ToString(),
        Overlap = e.Overlap.ToString(),
        Reliability = e.Reliability.ToString(),
        Tier = e.Tier.ToString(),
        Reason = e.Reason,
      };
  }

  private sealed record CycleDto
  {
    public required string Tier { get; init; }
    public required bool Firable { get; init; }
    public required bool TapRenewed { get; init; }
    public required bool CoCostsSatisfied { get; init; }
    public required bool Balanced { get; init; }
    public required bool Productive { get; init; }
    public string? LimitingReason { get; init; }
    public required IReadOnlyList<string> Edges { get; init; }

    [JsonIgnore]
    public string Signature => string.Join(";", Edges) + "#" + Tier;

    public static CycleDto Of(PortCycle c) =>
      new()
      {
        Tier = c.Tier.ToString(),
        Firable = c.Firable,
        TapRenewed = c.TapRenewed,
        CoCostsSatisfied = c.CoCostsSatisfied,
        Balanced = c.Balanced,
        Productive = c.Productive,
        LimitingReason = c.LimitingReason,
        // The cycle's edges, sorted — a stable signature regardless of the DFS traversal order.
        Edges = c.Edges.Select(EdgeDto.Of)
          .Select(e => e.Signature)
          .OrderBy(s => s, StringComparer.Ordinal)
          .ToList(),
      };
  }

  /// <summary>A stable, exhaustive string projection of an <see cref="ObjectFilter"/> — every facet the
  /// engine reads, in a fixed order, so two filters with the same content serialize identically and a
  /// changed facet shows in the snapshot diff.</summary>
  private static string? SubjectSignature(ObjectFilter? f)
  {
    if (f is null)
      return null;
    string list(IReadOnlyList<string>? xs) =>
      xs is null ? "" : string.Join(",", xs.OrderBy(x => x, StringComparer.Ordinal));
    return string.Join(
      "|",
      $"cardTypes={list(f.CardTypes)}",
      $"subtypes={list(f.Subtypes)}",
      $"supertypes={list(f.Supertypes)}",
      $"controller={f.Controller?.ToString() ?? ""}",
      $"isToken={f.IsToken?.ToString() ?? ""}",
      $"isSelf={f.IsSelf?.ToString() ?? ""}",
      $"excludeSelf={f.ExcludeSelf?.ToString() ?? ""}"
    );
  }

  // --- the tests ---

  [TestCaseSource(nameof(Sentinels))]
  public void Snapshot_matches(Sentinel sentinel)
  {
    var snapshotPath = Path.Combine(SnapshotsDir(), Slug(sentinel.Name) + ".json");
    Assert.That(
      File.Exists(snapshotPath),
      Is.True,
      $"Missing snapshot for sentinel '{sentinel.Name}' at {snapshotPath}. "
        + "Regenerate: dotnet test --filter \"FullyQualifiedName~Regenerate_snapshots\""
    );

    var actual = Canonical(Run(sentinel));
    var expected = File.ReadAllText(snapshotPath);

    Assert.That(
      Normalize(actual),
      Is.EqualTo(Normalize(expected)),
      $"Interaction-pipeline output for sentinel '{sentinel.Name}' changed. If this is intended, "
        + "regenerate the snapshots (the [Explicit] Regenerate_snapshots test) and JUSTIFY the diff in "
        + "your commit message — this is the cross-pillar regression guard (alignment initiative 03 #3)."
    );
  }

  /// <summary>Perturbation self-test: prove the comparison logic actually <b>detects</b> a changed
  /// projection. We take one sentinel's canonical output, then re-serialize a copy in which a single
  /// port Label has been mutated, and assert the two canonical strings differ. This exercises the diff
  /// mechanism (the snapshot's whole reason for existing), NOT the pipeline.</summary>
  [Test]
  public void Perturbation_changes_the_canonical_output()
  {
    var sentinel = LoadManifest().First(s => s.Kind == "combo");
    var output = Run(sentinel);

    var baseline = Canonical(output);

    // Mutate a single port's Label in a COPY of the graphs (records are immutable; `with` clones).
    var firstGraph = output.Graphs[0];
    Assert.That(firstGraph.Ports.Count, Is.GreaterThan(0), "sentinel must project at least one port");
    var mutatedPorts = firstGraph
      .Ports.Select((p, i) => i == 0 ? p with { Label = p.Label + "__PERTURBED" } : p)
      .ToList();
    var mutatedGraph = firstGraph with { Ports = mutatedPorts };
    var mutated = output with
    {
      Graphs = output.Graphs.Select((g, i) => i == 0 ? mutatedGraph : g).ToList(),
    };

    var perturbed = Canonical(mutated);

    Assert.That(
      Normalize(perturbed),
      Is.Not.EqualTo(Normalize(baseline)),
      "the canonical serializer must reflect a changed port Label — else the snapshot can't catch a "
        + "projection regression"
    );
    Assert.That(perturbed, Does.Contain("__PERTURBED"), "the mutation should appear in the output");
  }

  /// <summary>Determinism / round-trip stability: serializing the same output twice is byte-identical.
  /// (The committed snapshots rely on this — a second regen must produce no diff.)</summary>
  [Test]
  public void Canonical_serialization_is_byte_stable()
  {
    var sentinel = LoadManifest().First();
    var first = Canonical(Run(sentinel));
    var second = Canonical(Run(sentinel));
    Assert.That(second, Is.EqualTo(first), "canonical serialization must be deterministic");
  }

  /// <summary>Sanity: the manifest exercises every required ability family + projected discriminator,
  /// and totals ≥50 cards across the sentinels. A future edit that drops coverage fails loudly.</summary>
  [Test]
  public void Manifest_covers_the_families_and_at_least_fifty_cards()
  {
    var manifest = LoadManifest();
    var distinctCards = manifest
      .SelectMany(s => s.Cards.Select(c => c.Path))
      .Distinct(StringComparer.Ordinal)
      .Count();
    Assert.That(distinctCards, Is.GreaterThanOrEqualTo(50), "≥50 sentinel cards required");
    Assert.That(manifest.Any(s => s.Kind == "combo"), Is.True, "at least one combo sentinel required");

    // The projected ports of every sentinel, in aggregate, must touch every required label family.
    var allPorts = manifest
      .SelectMany(s => Run(s).Graphs.SelectMany(g => g.Ports))
      .ToList();
    var labels = allPorts.Select(p => p.Label).ToList();

    bool AnyLabel(Func<string, bool> pred) => labels.Any(pred);

    Assert.Multiple(() =>
    {
      // Projected effect discriminators.
      Assert.That(AnyLabel(l => l.StartsWith("emit:token", StringComparison.Ordinal)), Is.True, "createToken");
      Assert.That(AnyLabel(l => l.StartsWith("emit:mana", StringComparison.Ordinal)), Is.True, "addMana");
      Assert.That(AnyLabel(l => l.StartsWith("emit:counter", StringComparison.Ordinal)), Is.True, "putCounters");
      Assert.That(AnyLabel(l => l.StartsWith("emit:untap", StringComparison.Ordinal)), Is.True, "untap");
      Assert.That(AnyLabel(l => l.StartsWith("modify:pt", StringComparison.Ordinal)), Is.True, "modifyPT");
      Assert.That(AnyLabel(l => l.StartsWith("evasion:", StringComparison.Ordinal)), Is.True, "evasion");
      Assert.That(AnyLabel(l => l.StartsWith("replace:", StringComparison.Ordinal)), Is.True, "replacement");
      // Cost discriminators.
      Assert.That(AnyLabel(l => l.StartsWith("sac:", StringComparison.Ordinal)), Is.True, "sacrifice cost");
      Assert.That(AnyLabel(l => l.StartsWith("pay:mana", StringComparison.Ordinal)), Is.True, "mana cost");
      Assert.That(AnyLabel(l => l.StartsWith("tap:", StringComparison.Ordinal)), Is.True, "tap cost");
      // Trigger events.
      Assert.That(AnyLabel(l => l.StartsWith("ltb:", StringComparison.Ordinal)), Is.True, "Dies trigger");
      Assert.That(AnyLabel(l => l.StartsWith("etb:", StringComparison.Ordinal)), Is.True, "Enters trigger");
    });
  }

  [Test, Explicit("Writes all sentinel snapshot files to the source tree.")]
  public void Regenerate_snapshots()
  {
    var dir = SnapshotsDir();
    Directory.CreateDirectory(dir);
    var manifest = LoadManifest();
    foreach (var sentinel in manifest)
    {
      var path = Path.Combine(dir, Slug(sentinel.Name) + ".json");
      File.WriteAllText(path, Canonical(Run(sentinel)));
      TestContext.Out.WriteLine($"Wrote {path}");
    }
    TestContext.Out.WriteLine($"Wrote {manifest.Count} snapshots to {dir}");
  }

  // --- helpers ---

  private static string Normalize(string s) => s.Replace("\r\n", "\n").TrimEnd();

  /// <summary>Slugify a sentinel name into a stable, filesystem-safe snapshot filename.</summary>
  private static string Slug(string name)
  {
    var chars = name
      .ToLowerInvariant()
      .Select(c => char.IsLetterOrDigit(c) ? c : '-')
      .ToArray();
    var slug = new string(chars);
    while (slug.Contains("--"))
      slug = slug.Replace("--", "-");
    return slug.Trim('-');
  }

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above test dir).");
  }
}
