namespace MagicAtlas.Bench.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;

/// <summary>
/// The <b>safety gate</b> for the two-layer cycle engine (two-layer-cycle-engine.md, next-steps §3).
/// On EVERY bench-eligible combo (every Commander Spellbook combo all of whose cards have a gold
/// fixture), the two-layer <see cref="PortGraphEngine.FindCyclesByLabelGraph"/> result MUST be
/// <b>byte-identical in tiers</b> (and in the full cycle set) to the per-instance reference
/// <see cref="PortGraphEngine.FindCycles"/>. This is the proof the label-graph refactor is
/// behaviour-preserving: the expensive enumeration moved to the bounded atom graph, but the verdict it
/// produces is unchanged. A divergence here means the refactor changed behaviour and must NOT land
/// (the task's STOP-and-report criterion).
///
/// <para>The companion sentinel-set half of this gate lives in the MAST test project
/// (<c>PortWalkTwoLayerEquivalenceTest</c>), where the initiative-03 sentinel manifest is loaded.</para>
/// </summary>
[TestFixture]
public class TwoLayerEquivalenceTest
{
  private static GoldCorpus Corpus() => GoldCorpus.Load(BenchPaths.FixturesRoot);

  private static TypeOntology Ontology() =>
    JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(BenchPaths.OntologyPath))
    ?? throw new InvalidOperationException("Could not parse the type ontology");

  /// <summary>One test case per eligible combo, so a divergence names the exact combo.</summary>
  public static IEnumerable<TestCaseData> EligibleCombos()
  {
    var corpus = GoldCorpus.Load(BenchPaths.FixturesRoot);
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    foreach (var combo in snapshot.Combos.OrderBy(c => c.Id, StringComparer.Ordinal))
    {
      var cards = combo.Cards.Select(c => c.Name).Distinct(StringComparer.Ordinal).ToList();
      if (cards.Count < 2 || !cards.All(corpus.Contains))
        continue;
      yield return new TestCaseData(combo.Id, cards).SetName($"Equivalent_{Slug(combo.Id)}");
    }
  }

  [TestCaseSource(nameof(EligibleCombos))]
  public void Two_layer_is_byte_identical_to_per_instance(string comboId, IReadOnlyList<string> cards)
  {
    var ontology = Ontology();
    var corpus = Corpus();
    var walk = new PortWalk(ontology);
    var engine = new PortGraphEngine(ontology);

    var graphs = cards
      .Select(n => walk.Project(n, corpus.AbilitiesFor(n), corpus.ManaCostSymbolsFor(n)))
      .ToList();
    var edges = engine.Materialize(graphs);

    // The bench runs the engine at the cards-equivalent length bound 5. The reference enumeration is
    // hop-bounded; the two-layer's bound is the cards-based DISPLAY filter. We compare the UNBOUNDED
    // enumerations (the engine refactor's domain) — the display filter is a separate, post-enumeration
    // concern proven by CardsDisplayFilterTest.
    var reference = CycleSignatures(engine.FindCycles(edges));
    var twoLayer = CycleSignatures(engine.FindCyclesByLabelGraph(edges));

    Assert.That(
      twoLayer,
      Is.EqualTo(reference),
      $"two-layer engine diverged from the per-instance reference on combo {comboId} "
        + $"([{string.Join(", ", cards)}]). The refactor must be behaviour-preserving."
    );
  }

  /// <summary>The whole eligible set in one shot — a fast aggregate guard (the per-combo cases give the
  /// precise blame, this proves the gate covers the full bench at once).</summary>
  [Test]
  public void Two_layer_matches_across_the_whole_eligible_bench()
  {
    var ontology = Ontology();
    var corpus = Corpus();
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    var walk = new PortWalk(ontology);
    var engine = new PortGraphEngine(ontology);

    var diverged = new List<string>();
    var compared = 0;
    foreach (var combo in snapshot.Combos.OrderBy(c => c.Id, StringComparer.Ordinal))
    {
      var cards = combo.Cards.Select(c => c.Name).Distinct(StringComparer.Ordinal).ToList();
      if (cards.Count < 2 || !cards.All(corpus.Contains))
        continue;
      compared++;
      var graphs = cards
        .Select(n => walk.Project(n, corpus.AbilitiesFor(n), corpus.ManaCostSymbolsFor(n)))
        .ToList();
      var edges = engine.Materialize(graphs);
      var reference = CycleSignatures(engine.FindCycles(edges));
      var twoLayer = CycleSignatures(engine.FindCyclesByLabelGraph(edges));
      if (!twoLayer.SequenceEqual(reference))
        diverged.Add(combo.Id);
    }

    Assert.That(compared, Is.GreaterThan(0), "no eligible combos compared — the gate is vacuous");
    Assert.That(
      diverged,
      Is.Empty,
      $"two-layer diverged on {diverged.Count} combo(s): {string.Join(", ", diverged.Take(20))}"
    );
  }

  /// <summary>A stable, sorted projection of a cycle set: each cycle becomes its sorted-edge signature
  /// plus its tier and the §8 floor flags, so two sets compare byte-identical iff they carry the same
  /// cycles AT THE SAME TIERS. Order-independent (sorted) — the two engines may surface cycles in a
  /// different traversal order, which is not a behaviour change.</summary>
  private static IReadOnlyList<string> CycleSignatures(IReadOnlyList<PortCycle> cycles) =>
    cycles
      .Select(c =>
      {
        var hops = c
          .Edges.Select(e =>
            $"{e.From.Identity}=>{e.To.Identity}|{e.Provenance}|{e.Family}|{e.Overlap}|{e.Reliability}|{e.Tier}|{e.Reason}"
          )
          .OrderBy(s => s, StringComparer.Ordinal);
        return string.Join(
          ";",
          hops
        )
          + $"#tier={c.Tier};firable={c.Firable};tapRenewed={c.TapRenewed};coCosts={c.CoCostsSatisfied};balanced={c.Balanced};productive={c.Productive}";
      })
      .OrderBy(s => s, StringComparer.Ordinal)
      .ToList();

  private static string Slug(string s)
  {
    var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
    var slug = new string(chars);
    while (slug.Contains("--"))
      slug = slug.Replace("--", "-");
    return slug.Trim('-');
  }
}
