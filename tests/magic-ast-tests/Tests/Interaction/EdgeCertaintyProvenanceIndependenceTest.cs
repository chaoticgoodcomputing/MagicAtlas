namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR 0004 (salvaged piece 1) — <b>provenance must not leak into certainty</b>.
/// <para><see cref="PortEdge.Provenance"/> answers <em>how did we learn this edge</em>;
/// <see cref="PortEdge.Tier"/> answers <em>can we prove it holds</em>. The retired implementation
/// short-circuited the second with the first (<c>Provenance == CardDefined ? Green : …</c>), so an
/// intra-card edge was certain <em>by construction</em> rather than <em>by proof</em> — and a
/// card-defined edge that failed its own proof obligation still reported GREEN.</para>
/// <para>These are stateless invariants over the whole (Overlap × Reliability × Provenance) product,
/// not a sampled baseline: the exhaustive case below fails the moment any provenance branch is
/// reintroduced into the tier computation, for any verdict combination.</para>
/// </summary>
[TestFixture]
public class EdgeCertaintyProvenanceIndependenceTest
{
  private static PortNode Port(string card, string label, PortSide side) =>
    new()
    {
      Card = card,
      Label = label,
      Side = side,
      Identity = $"{card}::{label}",
    };

  private static PortEdge Edge(EdgeProvenance provenance, FilterRelation overlap, Trilean reliability) =>
    new()
    {
      From = Port("A", "sac:creature", PortSide.Consume),
      To = Port("A", "emit:token:creature", PortSide.Emit),
      Provenance = provenance,
      Overlap = overlap,
      Reliability = reliability,
    };

  /// <summary>
  /// The keystone invariant: for <b>every</b> operator verdict combination, the two provenances tier
  /// identically. Reintroducing <c>Provenance == CardDefined ? Green</c> fails this at the first
  /// non-(Overlaps, Yes) case — e.g. (Disjoint, Yes), which must be RED for both but would report GREEN
  /// for the card-defined half.
  /// </summary>
  [Test]
  public void Tier_is_identical_across_provenances_for_every_operator_verdict()
  {
    foreach (var overlap in Enum.GetValues<FilterRelation>())
      foreach (var reliability in Enum.GetValues<Trilean>())
      {
        var cardDefined = Edge(EdgeProvenance.CardDefined, overlap, reliability);
        var rulesDefined = Edge(EdgeProvenance.RulesDefined, overlap, reliability);
        Assert.That(
          cardDefined.Tier,
          Is.EqualTo(rulesDefined.Tier),
          $"provenance leaked into certainty at (Overlap={overlap}, Reliability={reliability}): "
            + $"CardDefined tiered {cardDefined.Tier} but RulesDefined tiered {rulesDefined.Tier}"
        );
      }
  }

  /// <summary>A card-defined edge whose subjects are provably Disjoint is a dark hop (RED), not a GREEN.</summary>
  [Test]
  public void Card_defined_provenance_does_not_certify_a_disjoint_edge()
  {
    var edge = Edge(EdgeProvenance.CardDefined, FilterRelation.Disjoint, Trilean.Yes);
    Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Red));
  }

  /// <summary>A card-defined edge with an undischarged reliability obligation floors AMBER (ADR 0003 §7 — nothing uncited is GREEN).</summary>
  [Test]
  public void Card_defined_provenance_does_not_certify_an_unknown_reliability_edge()
  {
    var edge = Edge(EdgeProvenance.CardDefined, FilterRelation.Overlaps, Trilean.Unknown);
    Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  /// <summary>
  /// The replacement for the shortcut: <see cref="PortEdge.CardDefined"/> <em>discharges</em> ADR 0003 §7's
  /// same-card witness and records the resulting verdict, so the GREEN is by proof. Same card → the card's
  /// own text witnesses the causal hop (CR 601.2h/602.2b cost payment; CR 608.2c resolution) → GREEN.
  /// </summary>
  [Test]
  public void Card_defined_factory_discharges_the_same_card_witness()
  {
    var edge = PortEdge.CardDefined(
      Port("Chatterfang", "sac:creature", PortSide.Consume),
      Port("Chatterfang", "emit:token:squirrel", PortSide.Emit)
    );
    Assert.Multiple(() =>
    {
      Assert.That(edge.Reliability, Is.EqualTo(Trilean.Yes));
      Assert.That(edge.Overlap, Is.EqualTo(FilterRelation.Overlaps));
      Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Green));
      Assert.That(edge.Reason, Is.Null);
    });
  }

  /// <summary>
  /// The second discharge — the <b>created-object witness</b>. Kiki-Jiki, Mirror Breaker's
  /// "{T}: Create a token that's a copy of target nonlegendary creature you control…" makes a token whose
  /// ETB is Corridor Monitor's "When Corridor Monitor enters, untap target artifact or creature."
  /// (both oracle texts read from the corpus fixtures, not from memory). The hop
  /// <c>Kiki-Jiki.emit:copy → copy.etb</c> has endpoints on two card identities, yet it is Kiki's own
  /// causality: the destination object exists only because this emit created it (CR 707.2 — the token
  /// copies the chosen permanent; CR 603.6a — it triggers its own ETB as it enters). The engine records
  /// that as <c>Grafter</c>, so the witness is structural and the edge certifies GREEN <em>by proof</em>.
  /// </summary>
  [Test]
  public void Card_defined_factory_discharges_the_created_object_witness()
  {
    var copyEmit = Port("Kiki-Jiki, Mirror Breaker", "emit:copy:creature", PortSide.Emit);
    var graftedEtb = Port("Kiki-Jiki, Mirror Breaker::copy::Corridor Monitor", "etb:creature:self", PortSide.Consume) with
    {
      Grafter = "Kiki-Jiki, Mirror Breaker",
      CopiedFrom = "Corridor Monitor",
    };

    var edge = PortEdge.CardDefined(copyEmit, graftedEtb);
    Assert.Multiple(() =>
    {
      Assert.That(edge.Reliability, Is.EqualTo(Trilean.Yes));
      Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Green));
    });

    // …and the witness is the Grafter link specifically, not "it spans two cards, so wave it through":
    // strip the Grafter and the very same pair of ports no longer certifies.
    var unwitnessed = PortEdge.CardDefined(copyEmit, graftedEtb with { Grafter = null });
    Assert.That(unwitnessed.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  /// <summary>
  /// The case the shortcut hid: an edge tagged card-defined between two unrelated cards has NO
  /// single card's text witnessing it and is not a created-object hop, so the obligation is undischarged
  /// and the edge is AMBER — the "Green by construction → Amber by proof" transition ADR 0004 anticipates.
  /// </summary>
  [Test]
  public void Card_defined_factory_refuses_to_certify_a_cross_card_hop()
  {
    var edge = PortEdge.CardDefined(
      Port("Chatterfang", "sac:creature", PortSide.Consume),
      Port("Pitiless Plunderer", "emit:token:treasure", PortSide.Emit)
    );
    Assert.Multiple(() =>
    {
      Assert.That(edge.Reliability, Is.EqualTo(Trilean.Unknown));
      Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Amber));
      Assert.That(edge.Reason, Does.Contain("no same-card"));
      Assert.That(edge.Reason, Does.Contain("created-object witness"));
    });
  }

  /// <summary>
  /// Regression guard for the real pipeline: the engine's materialized card-defined hops still certify
  /// GREEN after the shortcut's removal (they are same-card by construction), so removing the branch is
  /// a change of *justification*, not of *verdict*, for genuine intra-card causality.
  /// </summary>
  [Test]
  public void Materialized_card_defined_edges_are_still_green_by_proof()
  {
    var ontology = JsonSerializer.Deserialize<TypeOntology>(File.ReadAllText(TestData.OntologyPath))!;
    var pay = Port("A", "pay:mana:generic", PortSide.Consume);
    var emit = Port("A", "emit:mana:black", PortSide.Emit);
    var graph = new PortGraph
    {
      Ports = [pay, emit],
      CardDefinedEdges = [new() { From = pay, To = emit }],
    };

    var cardDefined = new PortGraphEngine(ontology)
      .Materialize([graph])
      .Where(e => e.Provenance == EdgeProvenance.CardDefined)
      .ToList();

    Assert.That(cardDefined, Is.Not.Empty);
    Assert.That(cardDefined.Select(e => e.Tier), Is.All.EqualTo(CertaintyTier.Green));
  }
}
