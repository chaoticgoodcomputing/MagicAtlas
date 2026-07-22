namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Interaction.Tests;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// The <b>live structural-mechanism harness</b> for ADR-0004 §2/Stage 6 (issue #34, the bijection's
/// soundness half). It runs the <b>real interaction engine</b> — <see cref="PortWalk"/> projection of the
/// committed parse golds, then <see cref="PortGraphEngine.Materialize"/> — over the interaction golds'
/// sentinel set, and reads the <see cref="PortEdge.Mechanism"/> / <see cref="PortEdge.Arm"/> tags off the
/// edges it forms.
///
/// <para><b>Why a live run, not a stored artifact.</b> §2/§5 are explicit that a gate reading its own
/// prior output "verifies the derivation against itself." So the mechanism inventory is <i>re-derived
/// in-process every run</i> by materializing the engine over the sentinels — exactly the harness
/// <c>PortWalkSentinelSnapshotTest.Run</c> uses, and hermetic for the same reason (every input is a
/// committed parse/interaction gold, so no corpus is required).</para>
///
/// <para><b>Zero rule ids.</b> Nothing here (or in the engine seam it reads) names a rollup rule. An edge's
/// structural mechanism is the <see cref="EdgeMechanism"/> + <see cref="PortFlowMatcher.FlowArm"/> the
/// engine recorded at formation, plus the endpoints' <see cref="PortStructure.Stem"/>s. The rule
/// attribution enters only on the golds' side, in the join.</para>
/// </summary>
public static class EdgeMechanismInventory
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  /// <summary>One structural mechanism the engine formed, keyed on nothing but structure: the coarse
  /// formation path, the fine arm, and the endpoints' stems. <see cref="Count"/> is how many live edges
  /// carried it — the non-vacuity evidence.</summary>
  public sealed record LiveMechanism(
    EdgeMechanism Mechanism,
    PortFlowMatcher.FlowArm? Arm,
    string FromStem,
    string ToStem,
    EdgeProvenance Provenance,
    int Count,
    IReadOnlyList<string> FiringSentinels
  )
  {
    /// <summary>The structural key two sides of the bijection meet on — no rule id, no gold id.</summary>
    public (EdgeMechanism, PortFlowMatcher.FlowArm?, string, string) Key => (Mechanism, Arm, FromStem, ToStem);

    /// <summary>A RulesDefined edge is engine behavior that a gold must witness (the soundness subject).
    /// A CardDefined edge is self-certifying (ADR-0003 §7 same-card / created-object witness), so it needs
    /// no external rule witness.</summary>
    public bool IsRulesDefined => Provenance == EdgeProvenance.RulesDefined;

    public string Describe() =>
      $"{Mechanism}{(Arm is { } a ? $"/{a}" : "")} [{FromStem} → {ToStem}]";
  }

  /// <summary>The live inventory over the whole sentinel set: distinct structural mechanisms + the run's
  /// own size counters, so a join that passed on an empty run is reported red.</summary>
  public sealed record Inventory(
    IReadOnlyList<LiveMechanism> Mechanisms,
    int SentinelsRun,
    int SentinelsProjected,
    int EdgesFormed
  )
  {
    public bool Vacuous => SentinelsProjected == 0 || EdgesFormed == 0 || Mechanisms.Count == 0;

    /// <summary>The distinct <see cref="PortFlowMatcher.FlowArm"/>s that actually fired.</summary>
    public IReadOnlyList<PortFlowMatcher.FlowArm> ArmsFired =>
      [.. Mechanisms.Where(m => m.Arm is not null).Select(m => m.Arm!.Value).Distinct().OrderBy(a => a.ToString(), StringComparer.Ordinal)];
  }

  /// <summary>Run the real engine over every sentinel and collect the distinct structural mechanisms it
  /// forms. Pure over the committed fixtures; deterministic (sentinels and mechanisms sorted).</summary>
  public static Inventory Derive()
  {
    var walk = new PortWalk(Ontology);
    var sentinels = SentinelSet.Derive();
    var byKey = new Dictionary<(EdgeMechanism, PortFlowMatcher.FlowArm?, string, string), (int Count, SortedSet<string> Firing, EdgeProvenance Prov)>();
    var projected = 0;
    var edgesFormed = 0;

    foreach (var sentinel in sentinels)
    {
      List<PortGraph> graphs;
      try
      {
        graphs = sentinel
          .Cards.Select(c =>
          {
            var gold = JsonNode.Parse(File.ReadAllText(Path.Combine(SentinelSet.FixturesDir(), c.Path)));
            var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
              ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
              ?["Symbols"];
            return walk.Project(c.Card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost);
          })
          .ToList();
      }
      catch (Exception)
      {
        continue; // a sentinel whose parse gold cannot project contributes no live edge (reported via counts)
      }

      projected++;
      var edges = new PortGraphEngine(Ontology).Materialize(graphs);
      foreach (var e in edges)
      {
        edgesFormed++;
        var key = (e.Mechanism, e.Arm, Stem(e.From), Stem(e.To));
        if (!byKey.TryGetValue(key, out var agg))
          agg = (0, new SortedSet<string>(StringComparer.Ordinal), e.Provenance);
        agg.Count++;
        agg.Firing.Add(sentinel.Name);
        byKey[key] = agg;
      }
    }

    var mechanisms = byKey
      .Select(kv => new LiveMechanism(
        kv.Key.Item1, kv.Key.Item2, kv.Key.Item3, kv.Key.Item4, kv.Value.Prov, kv.Value.Count, [.. kv.Value.Firing]))
      .OrderBy(m => m.Mechanism.ToString(), StringComparer.Ordinal)
      .ThenBy(m => m.Arm?.ToString() ?? "", StringComparer.Ordinal)
      .ThenBy(m => m.FromStem, StringComparer.Ordinal)
      .ThenBy(m => m.ToStem, StringComparer.Ordinal)
      .ToList();

    return new Inventory(mechanisms, sentinels.Count, projected, edgesFormed);
  }

  /// <summary>An endpoint's structural stem — the shared vocabulary the bijection joins on. A port with no
  /// <see cref="PortStructure"/> (an unconverted family) reports <c>"(none)"</c> so it never silently
  /// matches a declared stem.</summary>
  private static string Stem(PortNode p) => p.Structure?.Stem ?? "(none)";
}
