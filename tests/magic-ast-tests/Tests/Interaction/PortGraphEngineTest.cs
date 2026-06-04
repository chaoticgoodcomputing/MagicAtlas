namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0002 §4–7 — the <see cref="PortGraphEngine"/> reconstructs the canonical Chatterfang × Pitiless
/// free loop from the new single-role port model (via <see cref="PortWalk"/>), combining card-defined
/// edges (certain) with derived rules-defined edges (flow + the sac→death bridge + modifier). It must
/// land the <b>same sound AMBER</b> the old recognizer engine did — the Squirrel ⊄ creature straddle
/// on the death hop — proving the model migration preserves the verdict. Additive (S3a); the old
/// engine + golds stay green until S3b.
/// </summary>
[TestFixture]
public class PortGraphEngineTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortGraph Walk(string set, string file, string card)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      set,
      file
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    return new PortWalk(Ontology).Project(card, gold!["Output"]!["Oracle"]!["Abilities"]);
  }

  [Test]
  public void Reconstructs_the_chatterfang_pitiless_free_loop_as_amber()
  {
    var graphs = new[]
    {
      Walk("MH2", "Chatterfang.json", "Chatterfang"),
      Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    // The free loop, in port-hops: sac -(bridge)-> dies -(card)-> treasure -(modifier)-> replace
    //  -(card)-> squirrel -(flow)-> sac.
    var expected = new HashSet<string>
    {
      "Chatterfang::sac:creature:squirrel:controlled",
      "Pitiless Plunderer::ltb:creature:to-graveyard:controlled",
      "Pitiless Plunderer::emit:token:artifact:treasure:controlled",
      "Chatterfang::replace:token-creation",
      "Chatterfang::emit:token:creature:squirrel:controlled",
    };

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Select(e => e.From.Identity).ToHashSet().SetEquals(expected)
    );

    Assert.That(loop, Is.Not.Null, "the free loop should reconstruct from the parsed golds");

    // AMBER is the *correct, sound* verdict: the sac→dies bridge straddles Squirrel ⊄ creature
    // (a Squirrel could be a non-creature Kindred-Squirrel, CR 308.1), so the operator cannot certify
    // every sacrificed Squirrel satisfies a creature-death trigger. GREEN would be unsound.
    Assert.That(loop!.Tier, Is.EqualTo(CertaintyTier.Amber));

    var limiter = loop.LimitingHop!;
    Assert.That(limiter.From.Label, Is.EqualTo("sac:creature:squirrel:controlled"));
    Assert.That(limiter.To.Label, Is.EqualTo("ltb:creature:to-graveyard:controlled"));
    Assert.That(limiter.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(limiter.Reliability, Is.EqualTo(Trilean.Unknown));
    Assert.That(limiter.Reason, Is.EqualTo("Types"));
  }

  [Test]
  public void Card_defined_hops_are_green_by_construction()
  {
    var edges = new PortGraphEngine(Ontology).Materialize(
      new[] { Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer") }
    );
    var cardDefined = edges.First(e =>
      e.Provenance == EdgeProvenance.CardDefined
      && e.From.Label == "ltb:creature:to-graveyard:controlled"
    );
    Assert.That(cardDefined.To.Label, Is.EqualTo("emit:token:artifact:treasure:controlled"));
    Assert.That(cardDefined.Tier, Is.EqualTo(CertaintyTier.Green));
  }

  // ADR-0002 §9: a created Treasure is an object with its OWN ports — "{T}, Sacrifice this token: add
  // one mana of any color" (CR 111.10a) → an emit:mana:any fed by sacrificing the Treasure. That mana
  // then feeds Chatterfang's {B} cost (color-aware producer-choice), closing the combo's mana
  // sub-loop — pay:mana:black previously had no feeder, so the {B} half of the loop was invisible.
  [Test]
  public void Pitiless_treasure_resolves_to_mana_feeding_chatterfangs_black_cost()
  {
    var edges = new PortGraphEngine(Ontology).Materialize(
      new[]
      {
        Walk("MH2", "Chatterfang.json", "Chatterfang"),
        Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer"),
      }
    );
    Assert.That(
      edges.Any(e =>
        e.From.Label == "sac:artifact:treasure:controlled" && e.To.Label == "emit:mana:any"
      ),
      Is.True,
      "the Treasure's self-sacrifice should drive its emit:mana:any"
    );
    var manaHop = edges.FirstOrDefault(e =>
      e.From.Label == "emit:mana:any" && e.To.Label == "pay:mana:black"
    );
    Assert.That(manaHop, Is.Not.Null, "Treasure mana should feed Chatterfang's pay:mana:black");
    Assert.That(manaHop!.Tier, Is.EqualTo(CertaintyTier.Green)); // producer choice (CR — any color)
  }

  // The mana flow arm is colour-aware: any-colour mana (a Treasure) feeds any demand, a generic {N}
  // cost takes any colour, but a specific colour cannot pay a different colour (no green-pays-black).
  [Test]
  public void Mana_flow_is_colour_aware()
  {
    static PortNode Emit(string label) =>
      new()
      {
        Card = "T",
        Label = label,
        Side = PortSide.Emit,
        Identity = "T::" + label,
      };
    static PortNode Pay(string label) =>
      new()
      {
        Card = "U",
        Label = label,
        Side = PortSide.Consume,
        Identity = "U::" + label,
      };
    var any = Emit("emit:mana:any");
    var green = Emit("emit:mana:green");
    var payBlack = Pay("pay:mana:black");
    var payGeneric = Pay("pay:mana");

    var edges = new PortGraphEngine(Ontology).Materialize(
      [new PortGraph { Ports = [any, green, payBlack, payGeneric] }]
    );
    bool Flow(PortNode f, PortNode t) =>
      edges.Any(e => ReferenceEquals(e.From, f) && ReferenceEquals(e.To, t));

    Assert.That(Flow(any, payBlack), Is.True, "any-colour mana pays a black cost (producer choice)");
    Assert.That(Flow(any, payGeneric), Is.True, "any-colour mana pays a generic cost");
    Assert.That(Flow(green, payGeneric), Is.True, "green mana pays a generic cost");
    Assert.That(Flow(green, payBlack), Is.False, "green mana cannot pay a black cost");
  }

  // A created token refuels a sacrifice cost only if its AT-CREATION type satisfies the sac (CR 111.10:
  // a Treasure is a non-creature artifact). A later animation is an external enabler outside the
  // reconstructed loop — an engine policy, NOT an operator Disjoint claim (judge panel 2026-06-04:
  // operator subtype-exclusivity is unsound — crewed Vehicles, CR 301.7). Kills the corpus's
  // creature-token ↔ artifact-sac junk edges.
  [Test]
  public void Token_flow_refuels_a_sac_only_when_the_token_satisfies_it_at_creation()
  {
    static PortNode Emit(string label, ObjectFilter subj) =>
      new()
      {
        Card = "T",
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = "T::" + label,
      };
    static PortNode Sac(string label, ObjectFilter subj) =>
      new()
      {
        Card = "T",
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = "T::" + label,
      };

    // Created-token filters carry their type as a SUBTYPE with CardTypes null (the card-type in the
    // label is a display-time lift) — so the guard must lift subtype→card-type on both sides.
    var squirrelTok = Emit(
      "emit:token:creature:squirrel:controlled",
      new ObjectFilter { Subtypes = ["Squirrel"] }
    );
    var treasureTok = Emit(
      "emit:token:artifact:treasure:controlled",
      new ObjectFilter { Subtypes = ["Treasure"] }
    );
    var creatureSac = Sac("sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"] });
    var artifactSac = Sac("sac:artifact:controlled", new ObjectFilter { CardTypes = ["artifact"] });
    var treasureSac = Sac("sac:artifact:treasure:controlled", new ObjectFilter { Subtypes = ["Treasure"] });

    var edges = new PortGraphEngine(Ontology).Materialize(
      [
        new PortGraph
        {
          Ports = [squirrelTok, treasureTok, creatureSac, artifactSac, treasureSac],
        },
      ]
    );
    bool Flow(PortNode from, PortNode to) =>
      edges.Any(e => ReferenceEquals(e.From, from) && ReferenceEquals(e.To, to));

    // At-creation type match → the token refuels the sac.
    Assert.That(Flow(squirrelTok, creatureSac), Is.True, "a Squirrel refuels a creature-sac");
    Assert.That(Flow(treasureTok, artifactSac), Is.True, "a Treasure refuels an artifact-sac");
    Assert.That(Flow(treasureTok, treasureSac), Is.True, "a Treasure refuels a Treasure-sac");
    // At-creation type MISMATCH → no flow edge.
    Assert.That(Flow(treasureTok, creatureSac), Is.False, "a Treasure is not a creature at creation");
    Assert.That(Flow(squirrelTok, artifactSac), Is.False, "a Squirrel is not an artifact at creation");
    Assert.That(Flow(squirrelTok, treasureSac), Is.False, "a Squirrel is not a Treasure at creation");
  }

  // Firability (§8): a gated port floors the whole cycle to Amber even when every edge is Green.
  [Test]
  public void A_gated_cycle_floors_to_amber_even_with_green_edges()
  {
    var emit = new PortNode
    {
      Card = "A",
      Label = "emit:token:creature:squirrel:controlled",
      Side = PortSide.Emit,
      Identity = "A::emit",
    };
    var sac = new PortNode
    {
      Card = "A",
      Label = "sac:creature:squirrel:controlled",
      Side = PortSide.Consume,
      Identity = "A::sac",
    };
    PortCycle Cycle(PortNode e, PortNode s) =>
      new()
      {
        Edges =
        [
          new PortEdge { From = s, To = e, Provenance = EdgeProvenance.CardDefined }, // green
          new PortEdge
          {
            From = e,
            To = s,
            Provenance = EdgeProvenance.RulesDefined,
            Overlap = FilterRelation.Overlaps,
            Reliability = Trilean.Yes,
          }, // green
        ],
      };

    var gated = Cycle(emit with { Gated = true }, sac);
    Assert.That(gated.Firable, Is.False);
    Assert.That(gated.Tier, Is.EqualTo(CertaintyTier.Amber)); // floored from Green

    var ungated = Cycle(emit, sac);
    Assert.That(ungated.Firable, Is.True);
    Assert.That(ungated.Tier, Is.EqualTo(CertaintyTier.Green));
  }

  // Grammar-figure guard (ADR-0002 §2): every derived-edge family in known-families.json must be a
  // real wildcard pattern (PortLabel.Matches) that the canonical Chatterfang × Pitiless loop's ports
  // actually match — so the grammar stays a faithful description of the projection and can't drift
  // back to phantom names (sac-outlet/death-payoff) disconnected from the real colon-labels.
  [Test]
  public void Grammar_families_are_wildcard_patterns_that_real_loop_ports_match()
  {
    var families = JsonNode
      .Parse(
        File.ReadAllText(
          Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "Fixtures",
            "Interactions",
            "known-families.json"
          )
        )
      )!
      .AsArray();
    var patterns = families
      .SelectMany(f => new[] { f!["from"]!.ToString(), f!["to"]!.ToString() })
      .ToHashSet(StringComparer.Ordinal);

    var ports = new[]
    {
      Walk("MH2", "Chatterfang.json", "Chatterfang"),
      Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer"),
    }
      .SelectMany(g => g.Ports)
      .Select(p => p.Label)
      .ToList();

    foreach (var family in new[] { "sac:**", "ltb:**:to-graveyard:**", "replace:**" })
    {
      Assert.That(patterns, Does.Contain(family), $"grammar must declare the family {family}");
      Assert.That(
        ports.Any(l => PortLabel.Matches(family, l)),
        Is.True,
        $"a real canonical-loop port should match the grammar family {family}"
      );
    }
  }
}
