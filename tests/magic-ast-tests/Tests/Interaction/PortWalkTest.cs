namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0002 §4 — the <see cref="PortWalk"/> projects a card's real parsed AST into single-role ports
/// (via <see cref="PortLabel"/>) joined by card-defined edges. Proven on the canonical gold pair
/// (Chatterfang × Pitiless): the full six-port Chatterfang projection and Pitiless's trigger→emit,
/// with sides, quantities, and the intra-ability causality. Additive (S2a) — the old recognizer
/// projector + engine + golds stay untouched until the S3 migration.
/// </summary>
[TestFixture]
public class PortWalkTest
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
  public void Chatterfang_projects_its_six_single_role_ports()
  {
    var graph = Walk("MH2", "Chatterfang.json", "Chatterfang");
    Assert.That(
      graph.Ports.Select(p => p.Label),
      Is.EquivalentTo(
        new[]
        {
          "evasion:forestwalk",
          "replace:token-creation",
          "emit:token:creature:squirrel:controlled",
          "pay:mana:black",
          "sac:creature:squirrel:controlled",
          "modify:pt",
        }
      )
    );
  }

  [Test]
  public void Chatterfang_sides_and_quantities()
  {
    var graph = Walk("MH2", "Chatterfang.json", "Chatterfang");
    PortNode Port(string label) => graph.Ports.Single(p => p.Label == label);

    Assert.That(Port("replace:token-creation").Side, Is.EqualTo(PortSide.Intercept));
    Assert.That(Port("emit:token:creature:squirrel:controlled").Side, Is.EqualTo(PortSide.Emit));
    Assert.That(Port("pay:mana:black").Side, Is.EqualTo(PortSide.Consume));
    Assert.That(Port("pay:mana:black").Quantity, Is.EqualTo(1));
    // "Sacrifice X Squirrels" → a variable quantity → symbolic (null), floored to Amber-balance (§8).
    Assert.That(Port("sac:creature:squirrel:controlled").Quantity, Is.Null);
  }

  [Test]
  public void Chatterfang_card_defined_edges()
  {
    var graph = Walk("MH2", "Chatterfang.json", "Chatterfang");
    bool Edge(string from, string to) =>
      graph.CardDefinedEdges.Any(e => e.From.Label == from && e.To.Label == to);

    // The replacement intercept drives the Squirrel emit (one ability).
    Assert.That(Edge("replace:token-creation", "emit:token:creature:squirrel:controlled"), Is.True);
    // Both activated costs drive the P/T modification (one ability, two costs).
    Assert.That(Edge("pay:mana:black", "modify:pt"), Is.True);
    Assert.That(Edge("sac:creature:squirrel:controlled", "modify:pt"), Is.True);
    // The inert evasion ability has no consume, so no card-defined edge.
    Assert.That(graph.CardDefinedEdges.Any(e => e.To.Label == "evasion:forestwalk"), Is.False);
    Assert.That(graph.CardDefinedEdges.Count, Is.EqualTo(3));
  }

  [Test]
  public void Pitiless_projects_dies_trigger_emit_and_resolved_treasure_mana()
  {
    var graph = Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer");
    Assert.That(
      graph.Ports.Select(p => p.Label),
      Is.EquivalentTo(
        new[]
        {
          // the card's own dies-trigger → Treasure creation
          "ltb:creature:to-graveyard:controlled",
          "emit:token:artifact:treasure:controlled",
          // ADR-0002 §9: the created Treasure resolves to its intrinsic mana ability
          "sac:artifact:treasure:controlled",
          "tap:self",
          "emit:mana:any",
        }
      )
    );
    Assert.That(
      graph.Ports.Single(p => p.Label.StartsWith("ltb")).Side,
      Is.EqualTo(PortSide.Consume)
    );

    // The card's own causality: the dies-trigger drives the Treasure creation.
    Assert.That(
      graph.CardDefinedEdges.Any(e =>
        e.From.Label == "ltb:creature:to-graveyard:controlled"
        && e.To.Label == "emit:token:artifact:treasure:controlled"
      ),
      Is.True
    );
    // §9: the Treasure's self-sacrifice (and tap) drive its mana emit.
    Assert.That(
      graph.CardDefinedEdges.Any(e =>
        e.From.Label == "sac:artifact:treasure:controlled" && e.To.Label == "emit:mana:any"
      ),
      Is.True
    );
  }

  // The label names; the operator decides (§7) — so each non-scalar port carries its subject filter.
  [Test]
  public void Ports_carry_the_operators_subject_filter()
  {
    var graph = Walk("MH2", "Chatterfang.json", "Chatterfang");

    var sac = graph.Ports.Single(p => p.Label == "sac:creature:squirrel:controlled");
    Assert.That(sac.Subject!.Subtypes, Does.Contain("Squirrel"));
    Assert.That(sac.Subject!.Controller, Is.EqualTo(ControllerFilter.You)); // CR 701.21a treatment

    var emit = graph.Ports.Single(p => p.Label == "emit:token:creature:squirrel:controlled");
    Assert.That(emit.Subject!.IsToken, Is.True);
    Assert.That(emit.Subject!.Subtypes, Does.Contain("Squirrel"));

    // Inert ports carry no subject (scalar / no object).
    Assert.That(graph.Ports.Single(p => p.Label == "modify:pt").Subject, Is.Null);
  }

  // An "enters" trigger projects the canonical `etb` role, not the coarse `enters` fallback.
  [Test]
  public void Enters_trigger_projects_the_canonical_etb_role()
  {
    var abilities = JsonNode.Parse(
      """
      [{"Kind":"triggered",
        "Trigger":{"Event":"Enters","Filter":{"CardTypes":["creature"],"Controller":"You"}},
        "Effects":[{"EffectType":"drawCards","Count":{"QuantityType":"literal","Value":1}}]}]
      """
    );
    var graph = new PortWalk(Ontology).Project("Synthetic", abilities);
    Assert.That(graph.Ports.Select(p => p.Label), Does.Contain("etb:creature:controlled"));
    Assert.That(graph.Ports.Any(p => p.Label.StartsWith("enters")), Is.False);
  }

  // A structured (phase) event must never leak raw JSON into a label (the {\n "part" bug).
  [Test]
  public void Structured_phase_event_never_leaks_json_into_a_label()
  {
    var abilities = JsonNode.Parse(
      """
      [{"Kind":"triggered",
        "Trigger":{"Event":{"part":"Upkeep","edge":"Begin"}},
        "Effects":[{"EffectType":"drawCards","Count":{"QuantityType":"literal","Value":1}}]}]
      """
    );
    var graph = new PortWalk(Ontology).Project("Synthetic", abilities);
    Assert.That(graph.Ports.All(p => !p.Label.Contains('{')), "no raw JSON in any label");
    Assert.That(graph.Ports.Select(p => p.Label), Does.Contain("at:upkeep"));
  }

  // A rate-limited ("only once each turn") ability gates all its ports (ADR-0002 §8 firability).
  [Test]
  public void Rate_limited_ability_gates_its_ports()
  {
    var abilities = JsonNode.Parse(
      """
      [{"Kind":"activated","Restrictions":["OnlyOnceEachTurn"],
        "Costs":[{"CostType":"mana","Symbols":[{"Kind":"generic","GenericAmount":0}]}],
        "Effects":[{"EffectType":"createToken","Player":{"Kind":"You"},
          "Count":{"QuantityType":"literal","Value":1},
          "Token":{"Types":["creature"],"Subtypes":["Squirrel"]}}]}]
      """
    );
    var graph = new PortWalk(Ontology).Project("Synthetic", abilities);
    Assert.That(graph.Ports, Is.Not.Empty);
    Assert.That(graph.Ports.All(p => p.Gated), "once-each-turn gates every port of the ability");

    // An unrestricted card (Chatterfang) gates nothing.
    Assert.That(Walk("MH2", "Chatterfang.json", "Chatterfang").Ports.Any(p => p.Gated), Is.False);
  }

  // A {T} (tap) cost is a rate limit too (CR 107.5 — a permanent taps only once per untap), so a
  // persistent permanent's tap ability re-fires only with an untapper; absent one, a loop through it
  // isn't infinite. It gates all the ability's ports (ADR-0002 §8 firability), like once-each-turn.
  [Test]
  public void Tap_cost_gates_its_ports()
  {
    var abilities = JsonNode.Parse(
      """
      [{"Kind":"activated",
        "Costs":[{"CostType":"tap"}],
        "Effects":[{"EffectType":"addMana","Mana":"{C}"}]}]
      """
    );
    var graph = new PortWalk(Ontology).Project("Synthetic", abilities);
    Assert.That(graph.Ports, Is.Not.Empty);
    Assert.That(graph.Ports.Any(p => p.Label == "tap:self"));
    Assert.That(graph.Ports.All(p => p.Gated), "a {T} cost gates every port of the ability");
  }
}
