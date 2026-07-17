namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0003 Stage 3 — the <b>shadow-mode equivalence gate</b>. The engine's
/// <see cref="PortGraphEngine.FlowFeasible"/> (the ADR-0002 per-arm label switch) is the ORACLE; the new
/// <see cref="PortFlowMatcher.Captures"/> selects the flow arm from the structured <see cref="PortStructure"/>
/// alone and applies the same guard. This test runs BOTH over every emit×consume pair across the sentinel
/// corpus and asserts they render the identical accept/reject — the proof that the structured taxonomy
/// losslessly reproduces the engine's flow decisions (ADR-0003 §5 "quotient re-proof"), so matching can
/// move onto the structure at Stage 4 with no behaviour change.
///
/// <para>Includes the two frontend over-sensitivity cases as concrete anchors: a non-combat damage emit
/// must not feed a combat self-trigger (Barrage Ogre ✗→ Ancient Copper Dragon), and a token creation must
/// not feed a cast trigger (Chatterfang ✗→ Aang) — both rejected by the matcher exactly as the engine
/// rejects them.</para>
/// </summary>
[TestFixture]
public class PortFlowMatcherShadowTest
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

  [Test]
  public void Captures_equals_FlowFeasible_over_the_sentinel_corpus()
  {
    var graphs = SentinelGraphs();
    var engine = new PortGraphEngine(Ontology);
    var matcher = new PortFlowMatcher(engine);

    var ports = graphs.SelectMany(g => g.Ports).ToList();
    var emits = ports.Where(p => p.Side == PortSide.Emit).ToList();
    var consumes = ports.Where(p => p.Side == PortSide.Consume).ToList();

    var mismatches = new List<string>();
    var flowPairs = 0;
    foreach (var emit in emits)
      foreach (var consume in consumes)
      {
        var oracle = engine.FlowFeasible(emit, consume);
        var structured = matcher.Captures(emit, consume);
        if (oracle != structured)
          mismatches.Add(
            $"{emit.Card}::{emit.Label}  ->  {consume.Card}::{consume.Label} "
              + $"| FlowFeasible={oracle} Captures={structured} "
              + $"| emit.Structure={emit.Structure?.Canonical() ?? "<null>"} "
              + $"consume.Structure={consume.Structure?.Canonical() ?? "<null>"}"
          );
        if (oracle)
          flowPairs++;
      }

    Assert.Multiple(() =>
    {
      Assert.That(flowPairs, Is.GreaterThan(0), "expected some FlowFeasible-accepted pairs across the corpus");
      Assert.That(
        mismatches,
        Is.Empty,
        $"PortFlowMatcher.Captures diverged from the engine oracle on {mismatches.Count} pair(s):\n"
          + string.Join("\n", mismatches.Take(40))
      );
    });
  }

  // --- the two frontend over-sensitivity anchors, matched exactly as the engine matches them ---

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
      Assert.That(engine.FlowFeasible(barrage, copper), Is.False, "engine oracle agrees");
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
      Assert.That(engine.FlowFeasible(chatterfang, aang), Is.False, "engine oracle agrees");
    });
  }
}
