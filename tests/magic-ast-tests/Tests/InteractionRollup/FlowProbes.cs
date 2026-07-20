namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// ADR-0004 §1 — the <b>probe universe</b> an asserted-absence <c>no_arm[P]</c> claim is evaluated
/// against, and the evaluation itself.
///
/// <para><b>The vacuity trap this exists to avoid.</b> The naive absence claim is "this card produces zero
/// edges". For a single-card gold that is trivially true — there is no partner card — and it would keep
/// passing after somebody armed the port. So the claim is asserted against the <b>matcher</b>
/// (<see cref="PortFlowMatcher.SelectArm"/>), never against a materialized edge set: for the asserted port
/// <c>P</c>, no probe on the opposite side selects any arm.</para>
///
/// <para><b>The universe is read, never hardcoded.</b> Two derived sources, both computed at evaluation
/// time, so the assertion <em>strengthens as the taxonomy accretes</em>:
/// <list type="number">
///   <item><b>The rollup</b> (<c>port-topology.cited.json</c>, regenerated from the golds in-process) —
///     every <c>witnessed</c> stem, probed bare on the opposite side. A stem a future gold witnesses is in
///     this set the moment that gold lands, with nobody touching this file.</item>
///   <item><b>The live projection</b> — every distinct <see cref="PortStructure"/> the engine's own
///     families project over the sentinel corpus. These carry the <em>facets</em> the arms actually key on
///     (<c>creature[manner=sacrificed]</c>, <c>cast[role=trigger]</c>, <c>deployment:creature[event=etb]</c>),
///     which a bare stem cannot reach — an arm guarded by an attribute would otherwise slip past the
///     probe. This half also covers the standing dual-vocabulary fact that the rollup names three families
///     differently from the engine's stems (<c>damage-dealt</c> vs <c>damage</c>, <c>dice-rolled</c> vs
///     <c>dice</c>, <c>combat-presence</c> vs <c>combat</c>); with the rollup half alone, an arm keyed on
///     the engine's spelling would be invisible.</item>
/// </list>
/// The <see cref="ArmCoverage"/> self-test in <c>NoArmNonVacuityTests</c> is the standing proof that this
/// universe can in fact select arms — every arm the matcher has must be selectable by some probe pair, so
/// an absence claim can never pass merely because nothing in the universe could ever match.</para>
/// </summary>
public static class FlowProbes
{
  /// <summary>A probe: a candidate counterparty structure, plus where it came from (for failure messages).</summary>
  public sealed record Probe(PortStructure Structure, string Origin)
  {
    public override string ToString() => $"{Structure.Canonical()} ({Origin})";
  }

  // ── the live half: distinct structures the engine projects over the sentinel corpus ──────────────

  private static readonly Lazy<IReadOnlyList<Probe>> LiveProbes = new(BuildLiveProbes);

  private static string RepoRoot()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "nx.json")))
      dir = dir.Parent;
    return dir?.FullName
      ?? throw new InvalidOperationException("Could not locate repo root (no nx.json above test dir).");
  }

  private static IReadOnlyList<Probe> BuildLiveProbes()
  {
    var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(TestData.OntologyPath))!;
    var walk = new PortWalk(ontology);
    var cardsDir = Path.Combine(RepoRoot(), "tests", "magic-ast-tests", "Fixtures", "HandParsedCards");

    var byCanonical = new Dictionary<string, PortStructure>(StringComparer.Ordinal);
    var unstructured = new HashSet<string>(StringComparer.Ordinal);
    foreach (var path in Directory.EnumerateFiles(cardsDir, "*.json", SearchOption.AllDirectories))
    {
      var gold = JsonNode.Parse(File.ReadAllText(path));
      var abilities = gold?["Output"]?["Oracle"]?["Abilities"];
      var name = gold?["Output"]?["Name"]?.ToString();
      if (abilities is null || name is null)
        continue; // not a card gold (a manifest/regen artifact sharing the tree)
      var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
        ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
        ?["Symbols"];
      foreach (var p in walk.Project(name, abilities, manaCost).Ports)
        if (p.Structure is { } s)
          byCanonical.TryAdd(s.Canonical(), s);
        else
          unstructured.Add(CoarseKey(p.Side, p.Label));
    }

    UnstructuredLabelSet = unstructured;
    return byCanonical
      .OrderBy(kv => kv.Key, StringComparer.Ordinal)
      .Select(kv => new Probe(kv.Value, "live projection (hand-parsed card corpus)"))
      .ToList();
  }

  /// <summary>Every distinct structure the engine's families project over the hand-parsed card corpus.</summary>
  public static IReadOnlyList<Probe> Live => LiveProbes.Value;

  private static HashSet<string>? UnstructuredLabelSet;

  /// <summary>The key an UNSTRUCTURED (null-<see cref="PortStructure"/>) port is recorded under.</summary>
  public static string CoarseKey(PortSide side, string label) =>
    $"{side.ToString().ToLowerInvariant()} {label}";

  /// <summary>
  /// Every coarse label the engine projects with a NULL structure over the hand-parsed corpus — the
  /// totality fallback's own output. An asserted-absence gold whose port is unstructured records the coarse
  /// label it observed; this set is what makes that record executable. The moment someone writes an
  /// <c>IPortFamily</c> for that label (the realistic first step to arming it), the label LEAVES this set
  /// and the gold goes red — which is the change a probe keyed only on the gold's canonical stem would
  /// otherwise sail straight past (interaction-judge, 2026-07-20).
  /// </summary>
  public static IReadOnlyCollection<string> UnstructuredLabels
  {
    get
    {
      _ = LiveProbes.Value; // force the single projection pass that populates both sets
      return UnstructuredLabelSet!;
    }
  }

  /// <summary>
  /// The matcher-facing stem of a coarse label: the label minus its side prefix when it carries one
  /// (<c>emit:anynumberindeck</c> → <c>anynumberindeck</c>), else the label verbatim (<c>modify:pt</c>).
  /// A coarse label is not a stem — this is the honest guess at the stem a future family would key on, so
  /// the absence claim is asserted over BOTH vocabularies rather than only the gold's canonical spelling.
  /// </summary>
  public static string CoarseStem(PortSide side, string label)
  {
    var prefix = side.ToString().ToLowerInvariant() + ":";
    return label.StartsWith(prefix, StringComparison.Ordinal) ? label[prefix.Length..] : label;
  }

  /// <summary>The rollup's <c>witnessed</c> stems — the taxonomy half of the universe, read from the
  /// regenerated topology, never a literal list.</summary>
  public static IReadOnlyList<string> WitnessedStems(PortTopology topology) =>
    topology
      .Stems.Where(kv => kv.Value.Status == "witnessed")
      .Select(kv => kv.Key)
      .OrderBy(k => k, StringComparer.Ordinal)
      .ToList();

  /// <summary>
  /// The full probe set for one side: every witnessed rollup stem, probed bare, unioned with every live
  /// structure on that side. Side-agnostic on the rollup half deliberately — the rollup does not record
  /// which side witnessed a stem, and probing both sides can only ever make the absence claim stronger.
  /// </summary>
  public static IReadOnlyList<Probe> For(PortTopology topology, PortSide side)
  {
    var probes = new List<Probe>();
    var seen = new HashSet<string>(StringComparer.Ordinal);

    foreach (var stem in WitnessedStems(topology))
    {
      var s = PortStructure.Of(side, stem);
      if (seen.Add(s.Canonical()))
        probes.Add(new Probe(s, "rollup witnessed stem"));
    }

    foreach (var p in Live.Where(p => p.Structure.Side == side))
      if (seen.Add(p.Structure.Canonical()))
        probes.Add(p);

    return probes;
  }

  /// <summary>
  /// Evaluate <c>no_arm[P]</c>: every arm <see cref="PortFlowMatcher.SelectArm"/> selects between the
  /// asserted structure and some probe on the opposite side. EMPTY is the claim holding. An emit-side P is
  /// probed as <c>SelectArm(P, consume)</c>; a consume-side P as <c>SelectArm(emit, P)</c>.
  /// </summary>
  public static IReadOnlyList<(PortFlowMatcher.FlowArm Arm, Probe Counterparty)> ArmsFor(
    PortStructure asserted,
    IReadOnlyList<Probe> counterparties
  )
  {
    var hits = new List<(PortFlowMatcher.FlowArm, Probe)>();
    foreach (var c in counterparties)
    {
      var arm = asserted.Side == PortSide.Emit
        ? PortFlowMatcher.SelectArm(asserted, c.Structure)
        : PortFlowMatcher.SelectArm(c.Structure, asserted);
      if (arm is { } a)
        hits.Add((a, c));
    }
    return hits;
  }

  /// <summary>The arms the probe universe can select over ALL ordered probe pairs — the non-vacuity
  /// measure. See <c>NoArmNonVacuityTests</c>.</summary>
  public static IReadOnlyDictionary<PortFlowMatcher.FlowArm, string> ArmCoverage(PortTopology topology)
  {
    var emits = For(topology, PortSide.Emit);
    var consumes = For(topology, PortSide.Consume);
    var covered = new Dictionary<PortFlowMatcher.FlowArm, string>();
    foreach (var e in emits)
      foreach (var c in consumes)
        if (PortFlowMatcher.SelectArm(e.Structure, c.Structure) is { } arm && !covered.ContainsKey(arm))
          covered[arm] = $"{e.Structure.Canonical()} → {c.Structure.Canonical()}";
    return covered;
  }
}
