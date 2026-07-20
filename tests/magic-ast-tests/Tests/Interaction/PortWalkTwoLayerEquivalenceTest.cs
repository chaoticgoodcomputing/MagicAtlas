namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;
using Sentinel = MagicAST.Interaction.Tests.SentinelSet.Sentinel;

/// <summary>
/// The <b>safety gate</b> for the two-layer cycle engine (two-layer-cycle-engine.md, next-steps §3) over
/// the alignment initiative-03 <b>sentinel set</b>. For every sentinel (each combo + each single-card
/// family sentinel derived from the interaction golds by <see cref="SentinelSet"/>), the two-layer
/// <see cref="PortGraphEngine.FindCyclesByLabelGraph"/> result MUST be <b>byte-identical in tiers</b>
/// (and in the full cycle set) to the per-instance reference <see cref="PortGraphEngine.FindCycles"/>.
/// This is the proof the label-graph refactor is behaviour-preserving: the expensive enumeration moved to
/// the bounded atom graph, but the verdict it produces is unchanged. A divergence here means the refactor
/// changed behaviour and must NOT land (the task's STOP-and-report criterion).
///
/// <para>The companion bench half of this gate lives in the bench project
/// (<c>TwoLayerEquivalenceTest</c>), over every Commander-Spellbook eligible combo.</para>
/// </summary>
[TestFixture]
public class PortWalkTwoLayerEquivalenceTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  // --- the sentinel set (DERIVED from the interaction golds; see SentinelSet) ---

  private static IReadOnlyList<Sentinel> LoadManifest() => SentinelSet.Derive();

  public static IEnumerable<TestCaseData> Sentinels() =>
    LoadManifest().Select(s => new TestCaseData(s).SetName($"Equivalent_{Slug(s.Name)}"));

  // --- the gate ---

  [TestCaseSource(nameof(Sentinels))]
  public void Two_layer_is_byte_identical_to_per_instance(Sentinel sentinel)
  {
    var walk = new PortWalk(Ontology);
    var engine = new PortGraphEngine(Ontology);

    var graphs = sentinel
      .Cards.Select(c =>
      {
        var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(SentinelSet.FixturesDir(), c.Path)));
        var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
          ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
          ?["Symbols"];
        return walk.Project(c.Card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost);
      })
      .ToList();
    var edges = engine.Materialize(graphs);

    var reference = CycleSignatures(engine.FindCycles(edges));
    var twoLayer = CycleSignatures(engine.FindCyclesByLabelGraph(edges));

    Assert.That(
      twoLayer,
      Is.EqualTo(reference),
      $"two-layer engine diverged from the per-instance reference on sentinel '{sentinel.Name}'. "
        + "The refactor must be behaviour-preserving."
    );
  }

  /// <summary>The whole sentinel set in one shot — an aggregate guard alongside the per-sentinel cases.</summary>
  [Test]
  public void Two_layer_matches_across_the_whole_sentinel_set()
  {
    var walk = new PortWalk(Ontology);
    var engine = new PortGraphEngine(Ontology);

    var diverged = new List<string>();
    foreach (var sentinel in LoadManifest())
    {
      var graphs = sentinel
        .Cards.Select(c =>
        {
          var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(SentinelSet.FixturesDir(), c.Path)));
          var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
            ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
            ?["Symbols"];
          return walk.Project(c.Card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost);
        })
        .ToList();
      var edges = engine.Materialize(graphs);
      if (!CycleSignatures(engine.FindCyclesByLabelGraph(edges)).SequenceEqual(CycleSignatures(engine.FindCycles(edges))))
        diverged.Add(sentinel.Name);
    }

    Assert.That(
      diverged,
      Is.Empty,
      $"two-layer diverged on {diverged.Count} sentinel(s): {string.Join(", ", diverged.Take(20))}"
    );
  }

  /// <summary>A stable, sorted projection of a cycle set: each cycle becomes its sorted-edge signature
  /// plus its tier and the §8 floor flags, so two sets compare byte-identical iff they carry the same
  /// cycles AT THE SAME TIERS. Order-independent (sorted) — surfacing order is not behaviour.</summary>
  private static IReadOnlyList<string> CycleSignatures(IReadOnlyList<PortCycle> cycles) =>
    cycles
      .Select(c =>
        string.Join(
          ";",
          c.Edges.Select(e =>
              $"{e.From.Identity}=>{e.To.Identity}|{e.Provenance}|{e.Family}|{e.Overlap}|{e.Reliability}|{e.Tier}|{e.Reason}"
            )
            .OrderBy(s => s, StringComparer.Ordinal)
        )
          + $"#tier={c.Tier};firable={c.Firable};tapRenewed={c.TapRenewed};coCosts={c.CoCostsSatisfied};balanced={c.Balanced};productive={c.Productive}"
      )
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();

  private static string Slug(string name)
  {
    var chars = name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
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
