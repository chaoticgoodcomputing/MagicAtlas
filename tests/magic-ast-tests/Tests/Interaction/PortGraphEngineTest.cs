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

  // Multi-cost conjunction (§8): an ability fires only if ALL its costs are paid. A loop closing
  // through one cost port ("{B}, Sacrifice a creature: create a creature token" — the token refuels
  // the sac) is certifiable only if the ability's OTHER cost ports are fed too. With the {B} unfed
  // the loop floors to Amber; once a mana producer feeds pay:mana:black it can certify.
  [Test]
  public void A_loop_with_an_unfed_co_cost_floors_to_amber_until_it_is_fed()
  {
    static PortNode Consume(string label, ObjectFilter? subj) =>
      new()
      {
        Card = "A",
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = "A::" + label,
      };
    static PortNode Emit(string label, ObjectFilter? subj) =>
      new()
      {
        Card = "A",
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = "A::" + label,
      };

    var sac = Consume("sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"] });
    var payB = Consume("pay:mana:black", null);
    var tok = Emit(
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true }
    );
    CardDefinedEdge[] cardDefined = [new() { From = sac, To = tok }, new() { From = payB, To = tok }];
    var engine = new PortGraphEngine(Ontology);

    // {B} has no producer — the sac→token→sac loop exists but the ability can't actually fire.
    var unfed = engine.FindCycles(
      engine.Materialize([new PortGraph { Ports = [sac, payB, tok], CardDefinedEdges = cardDefined }])
    );
    var loopA = unfed.First(c => c.Edges.Any(e => e.From.Label == "sac:creature:controlled"));
    Assert.That(loopA.CoCostsSatisfied, Is.False);
    Assert.That(loopA.Tier, Is.EqualTo(CertaintyTier.Amber), "an unfed {B} co-cost floors the loop");

    // Add a mana producer: emit:mana:any → pay:mana:black feeds the co-cost (producer choice).
    var mana = Emit("emit:mana:any", null);
    var fed = engine.FindCycles(
      engine.Materialize(
        [new PortGraph { Ports = [sac, payB, tok, mana], CardDefinedEdges = cardDefined }]
      )
    );
    var loopB = fed.First(c => c.Edges.Any(e => e.From.Label == "sac:creature:controlled"));
    Assert.That(loopB.CoCostsSatisfied, Is.True);
    Assert.That(loopB.Tier, Is.EqualTo(CertaintyTier.Green), "with the {B} fed, the loop certifies");
  }

  // Balance (§8) end-to-end on the exemplar: the 2-card Chatterfang × Ruthless Knave loop is
  // mana-negative — Ruthless's {2}{B} (3 mana) exceeds the two Treasures it makes (2 mana) — so the
  // engine floors it to Amber (the third combo card supplies the missing mana). Pins the user's case
  // through the real parse → walk → §9 token-mana → balance pipeline.
  [Test]
  public void Chatterfang_x_ruthless_two_card_loop_is_mana_negative_amber()
  {
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(
      engine.Materialize(
        new[]
        {
          Walk("MH2", "Chatterfang.json", "Chatterfang"),
          Walk("XLN", "RuthlessKnave.json", "Ruthless Knave"),
        }
      ),
      maxLength: 5
    );

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Label == "emit:token:creature:squirrel:controlled" && e.To.Card == "Ruthless Knave")
      && c.Edges.All(e => e.From.Card is "Chatterfang" or "Ruthless Knave")
    );
    Assert.That(loop, Is.Not.Null, "the squirrel↔treasure loop should reconstruct");
    Assert.That(loop!.Balanced, Is.False, "{2}{B} cost (3) > two Treasures (2) — mana-negative");
    Assert.That(loop.Tier, Is.EqualTo(CertaintyTier.Amber));
    Assert.That(loop.LimitingReason, Is.EqualTo("mana-negative"));
  }

  // Balance (§8): a loop whose pay:mana cost exceeds the mana its own producers feed back is finite,
  // not infinite (Chatterfang × Ruthless Knave: {2}{B}=3 vs two Treasures=2). It floors to Amber until
  // the per-iteration mana production covers the cost.
  [Test]
  public void A_mana_negative_loop_floors_to_amber_until_production_covers_the_cost()
  {
    static PortNode Consume(string label, ObjectFilter? subj, int? qty = 1) =>
      new()
      {
        Card = "A",
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Quantity = qty,
        Identity = "A::" + label,
      };
    static PortNode Emit(string label, ObjectFilter? subj, int? qty = 1) =>
      new()
      {
        Card = "A",
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Quantity = qty,
        Identity = "A::" + label,
      };

    var sac = Consume("sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"] });
    var pay = Consume("pay:mana", null, 3); // the {3} co-cost
    var tok = Emit(
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true }
    );
    CardDefinedEdge[] cardDefined = [new() { From = sac, To = tok }, new() { From = pay, To = tok }];
    var engine = new PortGraphEngine(Ontology);

    // Production 2 < cost 3 → mana-negative → Amber.
    var shortMana = Emit("emit:mana:any", null, 2);
    var negative = engine.FindCycles(
      engine.Materialize(
        [new PortGraph { Ports = [sac, pay, tok, shortMana], CardDefinedEdges = cardDefined }]
      )
    );
    var loopA = negative.First(c => c.Edges.Any(e => e.From.Label == "sac:creature:controlled"));
    Assert.That(loopA.Balanced, Is.False);
    Assert.That(loopA.Tier, Is.EqualTo(CertaintyTier.Amber), "2 mana < {3} cost — finite, not infinite");

    // Production 3 ≥ cost 3 → balanced → Green.
    var enoughMana = Emit("emit:mana:any", null, 3);
    var balanced = engine.FindCycles(
      engine.Materialize(
        [new PortGraph { Ports = [sac, pay, tok, enoughMana], CardDefinedEdges = cardDefined }]
      )
    );
    var loopB = balanced.First(c => c.Edges.Any(e => e.From.Label == "sac:creature:controlled"));
    Assert.That(loopB.Balanced, Is.True);
    Assert.That(loopB.Tier, Is.EqualTo(CertaintyTier.Green), "3 mana covers the {3} cost");
  }

  // One-shot self-removal (§8, "B"): a cycle that traverses a source's OWN dies-trigger
  // (ltb:…:to-graveyard:self) is non-repeatable — the unique source dies once and the trigger fires
  // for that one death; the tokens it feeds back are different objects that can't re-satisfy the
  // self-trigger. So an Afterlife/Elenda-class self-death → token loop fed by a sac outlet is PRUNED
  // (structurally impossible), not floored to Amber. The control — the SAME shape with a non-self
  // ("another creature dies") trigger — is a real repeatable aristocrat, so it is retained.
  [Test]
  public void A_self_death_token_loop_is_pruned_as_one_shot()
  {
    static PortNode Consume(string card, string label, ObjectFilter subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
      };
    static PortNode Emit(string card, string label, ObjectFilter subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var engine = new PortGraphEngine(Ontology);
    // A free sac outlet on a second card (shared by both arms).
    var sac = Consume(
      "Outlet",
      "sac:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You }
    );
    bool ClosesAcrossOutlet(IReadOnlyList<PortCycle> cycles) =>
      cycles.Any(c => c.Edges.Any(e => e.From.Card == "Outlet" || e.To.Card == "Outlet"));

    // Afterlife/Elenda-class: "when this creature dies, create a creature token" (self-death).
    var selfDies = Consume(
      "Elenda",
      "ltb:creature:to-graveyard:self",
      new ObjectFilter { CardTypes = ["creature"], IsSelf = true }
    );
    var selfToken = Emit(
      "Elenda",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var selfGraph = new PortGraph
    {
      Ports = [selfDies, selfToken, sac],
      CardDefinedEdges = [new() { From = selfDies, To = selfToken }],
    };
    Assert.That(
      ClosesAcrossOutlet(engine.FindCycles(engine.Materialize([selfGraph]), maxLength: 5)),
      Is.False,
      "self-death → token + sac outlet is one-shot (the source dies once) — must be pruned, not surfaced"
    );

    // Control: a non-self ("another creature you control dies", Pitiless-style) trigger is a real
    // repeatable loop — retained — proving it is the self-ness that prunes, not the loop shape.
    var otherDies = Consume(
      "Pitiless",
      "ltb:creature:to-graveyard:controlled:another",
      new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      }
    );
    var otherToken = Emit(
      "Pitiless",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var otherGraph = new PortGraph
    {
      Ports = [otherDies, otherToken, sac],
      CardDefinedEdges = [new() { From = otherDies, To = otherToken }],
    };
    Assert.That(
      ClosesAcrossOutlet(engine.FindCycles(engine.Materialize([otherGraph]), maxLength: 5)),
      Is.True,
      "a non-self (another-creature) death trigger is a real repeatable loop — retained"
    );
  }

  // One-shot carve-out (§8, "B"): Persist/Undying — the SAME self-death also returns the source to the
  // battlefield (a card-defined ltb:…:self → emit:returntobattlefield), so it can die again and the
  // loop is NOT pruned (its finiteness then turns on counters, a separate axis). Without the carve-out
  // this would be wrongly pruned as one-shot.
  [Test]
  public void A_self_returning_source_is_not_pruned_as_one_shot()
  {
    static PortNode Consume(string card, string label, ObjectFilter? subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
      };
    static PortNode Emit(string card, string label, ObjectFilter? subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var engine = new PortGraphEngine(Ontology);
    var sac = Consume(
      "Outlet",
      "sac:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You }
    );
    // Persist-class: "when this dies, create a token AND return this to the battlefield."
    var selfDies = Consume(
      "Persistor",
      "ltb:creature:to-graveyard:self",
      new ObjectFilter { CardTypes = ["creature"], IsSelf = true }
    );
    var token = Emit(
      "Persistor",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var selfReturn = Emit("Persistor", "emit:returntobattlefield", null);
    var graph = new PortGraph
    {
      Ports = [selfDies, token, selfReturn, sac],
      CardDefinedEdges =
      [
        new() { From = selfDies, To = token },
        new() { From = selfDies, To = selfReturn },
      ],
    };

    var cycles = engine.FindCycles(engine.Materialize([graph]), maxLength: 5);
    Assert.That(
      cycles.Any(c => c.Edges.Any(e => e.From.Card == "Outlet" || e.To.Card == "Outlet")),
      Is.True,
      "a self-returning source (Persist/Undying) can die again — the loop is retained, not pruned"
    );
  }

  // One-shot self-sacrifice (§8 "A"): a created token can never satisfy a :self sacrifice ("Sacrifice
  // this") — the token is a different object, not the source (CR 400.7, the dual of the one-shot
  // self-death). So no flow edge refuels a self-sac, and a self-sacrificing producer (Chromatic Star,
  // Barrels of Blasting Jelly) can't have its loop falsely closed/certified. A generic "sacrifice an
  // artifact" is still refuelled by a created artifact token.
  [Test]
  public void A_created_token_does_not_refuel_a_self_sacrifice()
  {
    static PortNode Emit(string label, ObjectFilter subj) =>
      new()
      {
        Card = "Maker",
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = "Maker::" + label,
      };
    static PortNode Sac(string card, string label, ObjectFilter subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var token = Emit(
      "emit:token:artifact:treasure:controlled",
      new ObjectFilter
      {
        CardTypes = ["artifact"],
        Subtypes = ["Treasure"],
        IsToken = true,
        Controller = ControllerFilter.You,
      }
    );
    var selfSac = Sac(
      "SelfSaccer",
      "sac:artifact:self",
      new ObjectFilter
      {
        CardTypes = ["artifact"],
        IsSelf = true,
        Controller = ControllerFilter.You,
      }
    );
    var genericSac = Sac(
      "Outlet",
      "sac:artifact:controlled",
      new ObjectFilter { CardTypes = ["artifact"], Controller = ControllerFilter.You }
    );

    var edges = new PortGraphEngine(Ontology).Materialize(
      [new PortGraph { Ports = [token, selfSac, genericSac] }]
    );
    bool Flows(PortNode f, PortNode t) =>
      edges.Any(e => e.From.Identity == f.Identity && e.To.Identity == t.Identity);

    Assert.That(
      Flows(token, genericSac),
      Is.True,
      "a created Treasure refuels a generic 'sacrifice an artifact'"
    );
    Assert.That(
      Flows(token, selfSac),
      Is.False,
      "a created token is never the source of a 'Sacrifice this' — no refuel"
    );
  }

  // Per-colour balance (§8): a loop must produce each COLOURED pip its costs owe, not just the total.
  // Ant Queen ("{2}{G}: create a token") sacrificed to Ashnod's Altar ("Sacrifice a creature: {C}{C}"):
  // colorless can pay the generic {2} but never the {G} (CR 107.4) — so even three colorless can't make
  // it infinite. The balance must floor on the unpayable green pip, not certify on the fungible total.
  [Test]
  public void A_loop_whose_produced_colour_cannot_pay_a_coloured_pip_floors_to_amber()
  {
    static PortNode Consume(string card, string label, int? qty) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Quantity = qty,
        Identity = card + "::" + label,
      };
    static PortNode Emit(string card, string label, ObjectFilter? subj, int? qty) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Quantity = qty,
        Identity = card + "::" + label,
      };

    // Ant Queen: "{2}{G}: create a token" — generic {2} on the cycle, the {G} pip a co-cost (sibling).
    var payGeneric = Consume("Queen", "pay:mana", 2);
    var payGreen = Consume("Queen", "pay:mana:green", 1);
    var token = Emit(
      "Queen",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true },
      1
    );
    // Ashnod's Altar: "Sacrifice a creature: {C}{C}{C}" — three colorless (covers the TOTAL of 3).
    var sac = Consume("Altar", "sac:creature:controlled", 1);
    var colorless = Emit("Altar", "emit:mana:colorless", null, 3);
    sac = sac with { Subject = new ObjectFilter { CardTypes = ["creature"] } };

    CardDefinedEdge[] cardDefined =
    [
      new() { From = payGeneric, To = token },
      new() { From = payGreen, To = token },
      new() { From = sac, To = colorless },
    ];
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(
      engine.Materialize(
        [
          new PortGraph
          {
            Ports = [payGeneric, payGreen, token, sac, colorless],
            CardDefinedEdges = cardDefined,
          },
        ]
      )
    );
    var loop = cycles.First(c => c.Edges.Any(e => e.From.Label == "sac:creature:controlled"));
    Assert.That(
      loop.Balanced,
      Is.False,
      "3 colorless covers the {2} but never the {G} pip (CR 107.4) — not infinite"
    );
    Assert.That(loop.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  // Productivity (§8): a pure-mana loop must net POSITIVE mana — a 1-for-1 filter (Bog Initiate
  // {1}:Add{B} ↔ Farrelite Priest {1}:Add{W}) cycles the same mana forever, producing no advantage, so
  // it is a do-nothing, not an infinite combo. Net-zero pure-mana → Amber; net-positive → Green.
  [Test]
  public void A_net_zero_pure_mana_filter_is_not_productive_and_floors_to_amber()
  {
    static PortNode Pay(string card) =>
      new()
      {
        Card = card,
        Label = "pay:mana",
        Side = PortSide.Consume,
        Quantity = 1,
        Identity = card + "::pay:mana",
      };
    static PortNode Emit(string card, string colour, int qty) =>
      new()
      {
        Card = card,
        Label = "emit:mana:" + colour,
        Side = PortSide.Emit,
        Quantity = qty,
        Identity = card + "::emit:mana:" + colour,
      };

    var engine = new PortGraphEngine(Ontology);
    PortCycle TwoCardLoop(int bProduction)
    {
      var aPay = Pay("A");
      var aEmit = Emit("A", "black", 1);
      var bPay = Pay("B");
      var bEmit = Emit("B", "white", bProduction);
      var graphs = new[]
      {
        new PortGraph { Ports = [aPay, aEmit], CardDefinedEdges = [new() { From = aPay, To = aEmit }] },
        new PortGraph { Ports = [bPay, bEmit], CardDefinedEdges = [new() { From = bPay, To = bEmit }] },
      };
      var cycles = engine.FindCycles(engine.Materialize(graphs), maxLength: 5);
      return cycles.First(c =>
        c.Edges.Any(e => e.From.Card == "A") && c.Edges.Any(e => e.From.Card == "B")
      );
    }

    // 1-for-1 both ways: net-zero, pure mana → do-nothing → Amber.
    var netZero = TwoCardLoop(1);
    Assert.That(netZero.Productive, Is.False, "a 1-for-1 mana filter nets nothing");
    Assert.That(netZero.Tier, Is.EqualTo(CertaintyTier.Amber));

    // B makes 2 for 1 → net +1 mana per loop → a real infinite-mana engine → Green.
    var netPositive = TwoCardLoop(2);
    Assert.That(netPositive.Productive, Is.True);
    Assert.That(netPositive.Tier, Is.EqualTo(CertaintyTier.Green));
  }

  // Tap renewal (§8, "C-untap"): a tap gate is the dischargeable rate-limit — a loop that untaps the
  // permanent each iteration renews it. Blasting Station ("{T}, Sacrifice a creature: …" + "untap this
  // whenever a creature enters") is renewed by the very creature tokens its sac outlet consumes, so its
  // loop stays GREEN; strip the self-untap and the tap is a rate-limit with no untapper → Amber.
  [Test]
  public void A_tap_gate_is_discharged_when_the_loop_untaps_the_permanent()
  {
    static PortNode Consume(string card, string label, ObjectFilter subj, bool tap = false) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
        TapGated = tap,
      };
    static PortNode Emit(string card, string label, ObjectFilter? subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var creature = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You };
    // Blasting Station: a tap-gated sac outlet + a self-untap triggered by a creature entering.
    var sac = Consume("Station", "sac:creature:controlled", creature, tap: true);
    var dmg = Emit("Station", "emit:dealdamage", null); // inert payoff
    var etb = Consume("Station", "etb:creature", creature);
    // Token maker: when a creature dies, create a creature token (the loop's carrier).
    var dies = Consume("Maker", "ltb:creature:to-graveyard:controlled", creature);
    var token = Emit(
      "Maker",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var makerGraph = new PortGraph
    {
      Ports = [dies, token],
      CardDefinedEdges = [new() { From = dies, To = token }],
    };
    var engine = new PortGraphEngine(Ontology);

    // untapLabel: null = no untap; "emit:untap:self" = "untap this"; "emit:untap" = "untap target" (other).
    PortCycle Loop(string? untapLabel)
    {
      var ports = new List<PortNode> { sac, dmg };
      var cardDefined = new List<CardDefinedEdge> { new() { From = sac, To = dmg } };
      if (untapLabel is not null)
      {
        var untap = Emit("Station", untapLabel, null);
        ports.Add(etb);
        ports.Add(untap);
        cardDefined.Add(new() { From = etb, To = untap });
      }
      var station = new PortGraph { Ports = ports, CardDefinedEdges = cardDefined };
      return engine
        .FindCycles(engine.Materialize([station, makerGraph]), maxLength: 5)
        .First(c =>
          c.Edges.Any(e => e.From.Card == "Station") && c.Edges.Any(e => e.From.Card == "Maker")
        );
    }

    // "untap this" + the loop's creature tokens → self-untap fires each iteration → renewed → Green.
    var renewed = Loop("emit:untap:self");
    Assert.That(renewed.TapRenewed, Is.True);
    Assert.That(renewed.Firable, Is.True);
    Assert.That(renewed.Tier, Is.EqualTo(CertaintyTier.Green));

    // No untap → the tap is a rate-limit with no untapper → not firable → Amber.
    var noUntap = Loop(null);
    Assert.That(noUntap.TapRenewed, Is.False);
    Assert.That(noUntap.Tier, Is.EqualTo(CertaintyTier.Amber));
    Assert.That(noUntap.LimitingReason, Is.EqualTo("tap (not renewed by an untapper)"));

    // "untap TARGET permanent" (Corridor Monitor) renews someone ELSE, not the source's own tap — so the
    // tap is NOT renewed and the loop stays Amber. Guards the judge's false-GREEN.
    var targetUntap = Loop("emit:untap");
    Assert.That(targetUntap.TapRenewed, Is.False, "untapping a target is not a self-untap");
    Assert.That(targetUntap.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  // Bridge respects the loop's token type (§8): the sac→death bridge fires only if the object the loop
  // sacrifices can actually be the type the dies-trigger requires. A loop that sacrifices a Treasure
  // (artifact token, provably not a creature at creation — CR 111.10 / 110.4) into a "a creature you
  // control dies" trigger can never fire it, so the loop can't close → prune (Lithatog/Extruder ×
  // Pitiless). A creature token genuinely dies, so that loop is retained. Dual of the token→sac guard.
  [Test]
  public void A_loop_that_sacrifices_a_noncreature_token_into_a_creature_death_is_pruned()
  {
    static PortNode Consume(string card, string label, ObjectFilter subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
      };
    static PortNode Emit(string card, string label, ObjectFilter? subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var engine = new PortGraphEngine(Ontology);
    var you = ControllerFilter.You;
    PortGraph DiesMaker(string tokenLabel, ObjectFilter tokenSubj)
    {
      var dies = Consume(
        "Pitiless",
        "ltb:creature:to-graveyard:controlled",
        new ObjectFilter { CardTypes = ["creature"], Controller = you }
      );
      var token = Emit("Pitiless", tokenLabel, tokenSubj);
      return new PortGraph { Ports = [dies, token], CardDefinedEdges = [new() { From = dies, To = token }] };
    }
    PortGraph Outlet(string sacLabel, ObjectFilter sacSubj)
    {
      var sac = Consume("Outlet", sacLabel, sacSubj);
      var dmg = Emit("Outlet", "emit:dealdamage", null);
      return new PortGraph { Ports = [sac, dmg], CardDefinedEdges = [new() { From = sac, To = dmg }] };
    }
    bool ClosesAcrossOutlet(IReadOnlyList<PortCycle> cycles) =>
      cycles.Any(c => c.Edges.Any(e => e.From.Card == "Outlet" || e.To.Card == "Outlet"));

    // A Treasure (artifact token) sacrificed into a creature-dies trigger — the trigger can't fire.
    var treasureLoop = engine.FindCycles(
      engine.Materialize(
        [
          DiesMaker(
            "emit:token:artifact:treasure:controlled",
            new ObjectFilter { CardTypes = ["artifact"], Subtypes = ["Treasure"], IsToken = true, Controller = you }
          ),
          Outlet("sac:artifact:controlled", new ObjectFilter { CardTypes = ["artifact"], Controller = you }),
        ]
      ),
      maxLength: 5
    );
    Assert.That(
      ClosesAcrossOutlet(treasureLoop),
      Is.False,
      "a sacrificed Treasure isn't a creature, so the creature-dies trigger can't fire — prune"
    );

    // Control: a CREATURE token sacrificed into the same trigger genuinely dies → retained.
    var creatureLoop = engine.FindCycles(
      engine.Materialize(
        [
          DiesMaker(
            "emit:token:creature:controlled",
            new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = you }
          ),
          Outlet("sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"], Controller = you }),
        ]
      ),
      maxLength: 5
    );
    Assert.That(
      ClosesAcrossOutlet(creatureLoop),
      Is.True,
      "a sacrificed creature token genuinely dies — the loop is retained"
    );
  }

  // Counter-gate prune (§8): a death-trigger that requires the dying creature to have had a +1/+1
  // counter (Basri's Lieutenant) makes counter-less tokens, so a loop fed by those tokens can never
  // re-satisfy the gate — prune. Unless the loop has a PER-ITERATION counter source: a creature-enters →
  // put-a-counter trigger (Cathars' Crusade) counters each token before it dies. A one-time self-ETB
  // counter ("when THIS enters, put a counter") does NOT sustain the loop (it fires once).
  [Test]
  public void A_counter_gated_death_loop_with_no_per_iteration_counter_source_is_pruned()
  {
    static PortNode Consume(string card, string label, ObjectFilter subj, string? req = null) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Identity = card + "::" + label,
        RequiresCounter = req,
      };
    static PortNode Emit(string card, string label, ObjectFilter? subj) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Identity = card + "::" + label,
      };

    var you = ControllerFilter.You;
    var engine = new PortGraphEngine(Ontology);
    var knightSubj = new ObjectFilter
    {
      CardTypes = ["creature"],
      Subtypes = ["Knight"],
      IsToken = true,
      Controller = you,
    };
    bool ClosesAcrossOutlet(IReadOnlyList<PortCycle> cycles) =>
      cycles.Any(c => c.Edges.Any(e => e.From.Card == "Outlet" || e.To.Card == "Outlet"));

    // Basri's "had a +1/+1 counter" dies-trigger → a counter-less Knight, + a sac outlet. `counterEtb`
    // is the subject of Basri's own counter-add trigger: "self" = one-time (when Basri enters);
    // "creature" = per-iteration (when ANY creature enters → counters the loop's Knights).
    PortGraph Basri(string counterEtbScope)
    {
      var dies = Consume(
        "Basri",
        "ltb:creature:to-graveyard:controlled",
        new ObjectFilter { CardTypes = ["creature"], Controller = you },
        req: "+1/+1"
      );
      var knight = Emit("Basri", "emit:token:creature:knight:controlled", knightSubj);
      var etb = Consume(
        "Basri",
        counterEtbScope == "self" ? "etb:creature:controlled:self" : "etb:creature:controlled",
        new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = you,
          IsSelf = counterEtbScope == "self" ? true : null,
        }
      );
      var counter = Emit("Basri", "emit:counter:+1/+1:target", null);
      return new PortGraph
      {
        Ports = [dies, knight, etb, counter],
        CardDefinedEdges = [new() { From = dies, To = knight }, new() { From = etb, To = counter }],
      };
    }
    var sac = Consume("Outlet", "sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"], Controller = you });
    var dmg = Emit("Outlet", "emit:dealdamage", null);
    var outlet = new PortGraph { Ports = [sac, dmg], CardDefinedEdges = [new() { From = sac, To = dmg }] };

    // Only a one-time self-ETB counter → the Knights die counter-less → the gate can't re-fire → prune.
    Assert.That(
      ClosesAcrossOutlet(engine.FindCycles(engine.Materialize([Basri("self"), outlet]), maxLength: 5)),
      Is.False,
      "a one-time self-ETB counter doesn't sustain the loop — the 'had a counter' gate can't re-fire"
    );

    // A per-iteration creature-ETB counter source counters each Knight before it dies → retained.
    Assert.That(
      ClosesAcrossOutlet(engine.FindCycles(engine.Materialize([Basri("creature"), outlet]), maxLength: 5)),
      Is.True,
      "a creature-enters → counter source makes the gate satisfiable each iteration — retained"
    );
  }

  // Phase-10 conjunction tightening (§8): a co-cost is satisfied only if fed BY THE LOOP (a producer on
  // a cycle card), not by a corpus-global producer. A Ruthless-Knave-shaped ability ("{1} + Sacrifice a
  // creature: make a Treasure") whose sac:creature is fed only by an OFF-CYCLE creature-maker can't
  // actually fire each iteration (the loop makes only Treasures) → Amber. Move the creature source onto
  // a cycle card → fed by the loop → Green.
  [Test]
  public void A_co_cost_is_satisfied_only_when_fed_by_a_cycle_card()
  {
    static PortNode C(string card, string label, ObjectFilter? subj, int? qty = 1) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Consume,
        Subject = subj,
        Quantity = qty,
        Identity = card + "::" + label,
      };
    static PortNode E(string card, string label, ObjectFilter? subj, int? qty = 1) =>
      new()
      {
        Card = card,
        Label = label,
        Side = PortSide.Emit,
        Subject = subj,
        Quantity = qty,
        Identity = card + "::" + label,
      };

    var you = ControllerFilter.You;
    var engine = new PortGraphEngine(Ontology);
    var treasure = new ObjectFilter { CardTypes = ["artifact"], Subtypes = ["Treasure"], IsToken = true, Controller = you };
    var creatureTok = new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = you };

    // P: "{1}, Sacrifice a creature: create a Treasure" — the sac:creature is a co-cost of pay:mana.
    var pPay = C("P", "pay:mana", null, 1);
    var pSac = C("P", "sac:creature:controlled", new ObjectFilter { CardTypes = ["creature"], Controller = you });
    var pTreasure = E("P", "emit:token:artifact:treasure:controlled", treasure);
    CardDefinedEdge[] pEdges = [new() { From = pPay, To = pTreasure }, new() { From = pSac, To = pTreasure }];
    // Q: sac the Treasure → mana, closing the mana loop P↔Q.
    var qSac = C("Q", "sac:artifact:treasure:controlled", new ObjectFilter { CardTypes = ["artifact"], Subtypes = ["Treasure"], Controller = you });
    var qMana = E("Q", "emit:mana:any", null, 1);
    CardDefinedEdge[] qEdges = [new() { From = qSac, To = qMana }];
    var pGraph = new PortGraph { Ports = [pPay, pSac, pTreasure], CardDefinedEdges = pEdges };

    PortCycle PQ(params PortGraph[] more)
    {
      var graphs = new List<PortGraph> { pGraph };
      graphs.AddRange(more);
      return engine
        .FindCycles(engine.Materialize(graphs), maxLength: 5)
        .First(c => c.Edges.Any(e => e.From.Card == "P") && c.Edges.Any(e => e.From.Card == "Q"));
    }

    // Off-cycle creature-maker R feeds P's sac:creature — NOT a loop producer → co-cost unfed → Amber.
    var qGraph = new PortGraph { Ports = [qSac, qMana], CardDefinedEdges = qEdges };
    var rGraph = new PortGraph { Ports = [E("R", "emit:token:creature:controlled", creatureTok)] };
    var amber = PQ(qGraph, rGraph);
    Assert.That(amber.CoCostsSatisfied, Is.False, "a co-cost fed only off-cycle is not fed by the loop");
    Assert.That(amber.Tier, Is.EqualTo(CertaintyTier.Amber));

    // The cycle card Q makes the creature → fed by the loop → satisfied → Green.
    var qGraphWithCreature = new PortGraph
    {
      Ports = [qSac, qMana, E("Q", "emit:token:creature:controlled", creatureTok)],
      CardDefinedEdges = qEdges,
    };
    var green = PQ(qGraphWithCreature);
    Assert.That(green.CoCostsSatisfied, Is.True, "a co-cost fed by a cycle-card producer is satisfied");
    Assert.That(green.Tier, Is.EqualTo(CertaintyTier.Green));
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
