namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0003 — the structured flow matcher regression guard. Since the Stage-4 cutover
/// <see cref="PortFlowMatcher.Captures"/> is authoritative (the ADR-0002 <c>FlowFeasible</c> label switch
/// that once shadowed it has been deleted; the quotient-equivalence proof is banked in git history + the
/// ADR). This fixture now pins <see cref="PortFlowMatcher.Captures"/>'s decisions directly:
///
/// <list type="bullet">
/// <item>the set of accepted emit×consume pairs over the sentinel corpus is snapshotted to
/// <c>Snapshots/captures-accepted-pairs.txt</c> (regenerate with <c>UPDATE_SNAPSHOTS=1</c>) — a stateless
/// golden that makes any flow-adjacency drift a loud, reviewable diff (e.g. the ADR-0003 §5 sac→removal
/// remodel is expected to grow this set);</item>
/// <item>the two frontend over-sensitivity cases stay rejected: a non-combat damage emit must not feed a
/// combat self-trigger (Barrage Ogre ✗→ Ancient Copper Dragon), and a token creation must not feed a cast
/// trigger (Chatterfang ✗→ Aang).</item>
/// </list>
/// </summary>
[TestFixture]
public class PortFlowMatcherTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName ?? throw new InvalidOperationException("no nx.json above test dir");
  }

  private static string FixturesDir() => Path.Combine(RepoRoot(), "tests", "magic-ast-tests", "Fixtures");

  private static string SnapshotPath() =>
    Path.Combine(
      RepoRoot(), "tests", "magic-ast-tests", "Tests", "Interaction", "Snapshots",
      "captures-accepted-pairs.txt"
    );

  private static IReadOnlyList<PortGraph> SentinelGraphs()
  {
    var manifestPath = Path.Combine(
      RepoRoot(), "tests", "magic-ast-tests", "Tests", "Interaction", "Snapshots", "sentinels.json"
    );
    var root = JsonNode.Parse(File.ReadAllText(manifestPath))!;
    var walk = new PortWalk(Ontology);
    var seen = new HashSet<string>(StringComparer.Ordinal);
    var graphs = new List<PortGraph>();
    foreach (var e in root["entries"]!.AsArray())
      foreach (var c in e!["cards"]!.AsArray())
      {
        var path = c!["path"]!.ToString();
        if (!seen.Add(path))
          continue;
        var card = c!["card"]!.ToString();
        var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(FixturesDir(), path)));
        var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
          ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
          ?["Symbols"];
        graphs.Add(walk.Project(card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost));
      }
    return graphs;
  }

  private static string PairKey(PortNode emit, PortNode consume) =>
    $"{emit.Card}::{emit.Structure?.Canonical() ?? "<null>"}"
    + $"  ->  {consume.Card}::{consume.Structure?.Canonical() ?? "<null>"}";

  [Test]
  public void Captures_accepts_the_snapshotted_pairs_over_the_sentinel_corpus()
  {
    var graphs = SentinelGraphs();
    var engine = new PortGraphEngine(Ontology);
    var matcher = new PortFlowMatcher(engine);

    var ports = graphs.SelectMany(g => g.Ports).ToList();
    var emits = ports.Where(p => p.Side == PortSide.Emit).ToList();
    var consumes = ports.Where(p => p.Side == PortSide.Consume).ToList();

    var accepted = new List<string>();
    foreach (var emit in emits)
      foreach (var consume in consumes)
        if (matcher.Captures(emit, consume))
          accepted.Add(PairKey(emit, consume));
    accepted.Sort(StringComparer.Ordinal);

    var actual = string.Join("\n", accepted);
    var path = SnapshotPath();

    if (Environment.GetEnvironmentVariable("UPDATE_SNAPSHOTS") == "1" || !File.Exists(path))
    {
      File.WriteAllText(path, actual + "\n");
      Assert.Pass($"wrote {accepted.Count} accepted pair(s) to {path}");
    }

    var expected = File.ReadAllText(path).TrimEnd('\n');
    Assert.Multiple(() =>
    {
      Assert.That(accepted, Is.Not.Empty, "expected some Captures-accepted pairs across the sentinel corpus");
      Assert.That(
        actual,
        Is.EqualTo(expected),
        "PortFlowMatcher.Captures flow-adjacency drifted from the committed snapshot. If intended (e.g. a "
          + "new/retired flow arm), review the diff and regenerate with UPDATE_SNAPSHOTS=1."
      );
    });
  }

  // --- the two frontend over-sensitivity anchors, matched exactly as the matcher matches them ---

  private static PortNode Port(string card, string label, PortSide side, ObjectFilter? subject) =>
    PortFamilyRegistry.Annotate(
      new PortNode
      {
        Identity = $"{card}::{label}",
        Card = card,
        Label = label,
        Side = side,
        Subject = subject,
      },
      Ontology
    );

  [Test]
  public void Noncombat_damage_does_not_feed_a_combat_self_trigger()
  {
    // Barrage Ogre: "This creature deals 2 damage to any target." — noncombat.
    var barrage = Port("Barrage Ogre", "emit:damage:noncombat:any", PortSide.Emit,
      new ObjectFilter { IsSelf = true });
    // Ancient Copper Dragon: "Whenever this creature deals combat damage to a player…" — combat, self-source.
    var copper = Port("Ancient Copper Dragon", "trigger:damage:combat:player", PortSide.Consume,
      new ObjectFilter { IsSelf = true });

    var engine = new PortGraphEngine(Ontology);
    var matcher = new PortFlowMatcher(engine);

    Assert.Multiple(() =>
    {
      Assert.That(copper.Structure, Is.Not.Null, "the damage trigger must be structured");
      Assert.That(PortFlowMatcher.SelectArm(barrage.Structure!, copper.Structure!),
        Is.EqualTo(PortFlowMatcher.FlowArm.DamageToTrigger), "the pair does select the damage arm…");
      Assert.That(matcher.Captures(barrage, copper), Is.False, "…but the combat-manner guard rejects it");
    });
  }

  [Test]
  public void Token_creation_does_not_feed_a_cast_trigger()
  {
    // Chatterfang makes a Squirrel creature token.
    var chatterfang = Port("Chatterfang, Squirrel General", "emit:token:creature:squirrel:controlled",
      PortSide.Emit, new ObjectFilter { CardTypes = ["creature"], Subtypes = ["squirrel"], Controller = ControllerFilter.You });
    // Aang: "Whenever you cast a Lesson spell…"
    var aang = Port("Aang, the Last Airbender", "trigger:cast:spell:lesson:controlled", PortSide.Consume,
      new ObjectFilter { Subtypes = ["lesson"], Controller = ControllerFilter.You });

    var engine = new PortGraphEngine(Ontology);
    var matcher = new PortFlowMatcher(engine);

    Assert.Multiple(() =>
    {
      Assert.That(PortFlowMatcher.SelectArm(chatterfang.Structure!, aang.Structure!),
        Is.Null, "token creation and a cast trigger share no flow arm — pruned structurally");
      Assert.That(matcher.Captures(chatterfang, aang), Is.False);
    });
  }
}
