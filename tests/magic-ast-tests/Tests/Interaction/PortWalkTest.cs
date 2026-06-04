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
  public void Pitiless_projects_trigger_and_emit_with_one_edge()
  {
    var graph = Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer");
    Assert.That(
      graph.Ports.Select(p => p.Label),
      Is.EquivalentTo(
        new[] { "ltb:creature:to-graveyard:controlled", "emit:token:artifact:treasure:controlled" }
      )
    );
    Assert.That(
      graph.Ports.Single(p => p.Label.StartsWith("ltb")).Side,
      Is.EqualTo(PortSide.Consume)
    );

    var edge = graph.CardDefinedEdges.Single();
    Assert.That(edge.From.Label, Is.EqualTo("ltb:creature:to-graveyard:controlled"));
    Assert.That(edge.To.Label, Is.EqualTo("emit:token:artifact:treasure:controlled"));
  }
}
