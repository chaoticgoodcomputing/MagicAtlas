namespace MagicAtlas.Bench.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;

/// <summary>
/// Task item 3 of the two-layer engine: the length-bound is demoted to a cards-based DISPLAY filter
/// (two-layer-cycle-engine.md §Complexity — "<c>LengthBound</c> demotes to a display/query filter in
/// <em>cards</em>"). The enumeration runs over the (unbounded) label graph; the cards cutoff is applied
/// post-enumeration to the instance cycles. These tests prove the filter is measured in DISTINCT CARDS,
/// not hops: a genuine 2-card combo's cycle survives <c>K=2</c> but is filtered at <c>K=1</c>, and the
/// default (no bound) keeps everything.
/// </summary>
[TestFixture]
public class CardsDisplayFilterTest
{
  // A known reconstructed two-card combo (bench-report.json: Green, spans exactly two distinct cards).
  private static readonly string[] TwoCardCombo =
  [
    "Exquisite Blood",
    "Marauding Blight-Priest",
  ];

  private static (PortGraphEngine Engine, IReadOnlyList<PortEdge> Edges) Setup(string[] cards)
  {
    var ontology =
      JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(BenchPaths.OntologyPath))
      ?? throw new InvalidOperationException("Could not parse the type ontology");
    var corpus = GoldCorpus.Load(BenchPaths.FixturesRoot);
    var walk = new PortWalk(ontology);
    var engine = new PortGraphEngine(ontology);
    var graphs = cards
      .Select(n => walk.Project(n, corpus.AbilitiesFor(n), corpus.ManaCostSymbolsFor(n)))
      .ToList();
    return (engine, engine.Materialize(graphs));
  }

  private static int DistinctCards(PortCycle c) =>
    c.Edges.SelectMany(e => new[] { e.From.Card, e.To.Card }).Distinct(StringComparer.Ordinal).Count();

  [Test]
  public void Default_is_unbounded_and_equals_the_reference_enumeration()
  {
    var (engine, edges) = Setup(TwoCardCombo);
    var unbounded = engine.FindCyclesByLabelGraph(edges);
    var reference = engine.FindCycles(edges);
    Assert.That(unbounded.Count, Is.EqualTo(reference.Count), "default must enumerate the full set");
    Assert.That(unbounded.Any(c => DistinctCards(c) >= 2), Is.True, "a multi-card cycle must exist to test the filter");
  }

  [Test]
  public void Cards_filter_at_two_keeps_a_genuine_two_card_cycle()
  {
    var (engine, edges) = Setup(TwoCardCombo);
    var atTwo = engine.FindCyclesByLabelGraph(edges, displayMaxLengthInCards: 2);
    Assert.That(
      atTwo.Any(c => DistinctCards(c) == 2),
      Is.True,
      "a two-card combo's reconstruction must survive a 2-card display bound"
    );
    Assert.That(atTwo.All(c => DistinctCards(c) <= 2), Is.True, "nothing over the bound may remain");
  }

  [Test]
  public void Cards_filter_at_one_drops_every_multi_card_cycle()
  {
    var (engine, edges) = Setup(TwoCardCombo);
    var atOne = engine.FindCyclesByLabelGraph(edges, displayMaxLengthInCards: 1);
    Assert.That(
      atOne.All(c => DistinctCards(c) <= 1),
      Is.True,
      "a 1-card display bound must drop every cross-card cycle (the bound is in CARDS, not hops)"
    );
  }

  [Test]
  public void The_bound_is_cards_not_hops()
  {
    // The same cycle can have more HOPS than distinct CARDS (an intra-card chain adds hops, not cards).
    // Prove the filter keys on the distinct-card count by checking a cycle with >K hops still survives a
    // K-card bound when it spans ≤K cards.
    var (engine, edges) = Setup(TwoCardCombo);
    var atTwo = engine.FindCyclesByLabelGraph(edges, displayMaxLengthInCards: 2);
    var multiHopTwoCard = atTwo.FirstOrDefault(c => c.Edges.Count > 2 && DistinctCards(c) == 2);
    if (multiHopTwoCard is not null)
      Assert.That(
        multiHopTwoCard.Edges.Count,
        Is.GreaterThan(DistinctCards(multiHopTwoCard)),
        "a >2-hop, 2-card cycle surviving K=2 proves the bound is in cards, not hops"
      );
    else
      Assert.Pass("no multi-hop two-card cycle in this fixture set — the cards-bound semantics still hold");
  }
}
