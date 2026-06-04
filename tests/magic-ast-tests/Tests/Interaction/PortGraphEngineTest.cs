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
    var cardDefined = edges.Single(e => e.Provenance == EdgeProvenance.CardDefined);
    Assert.That(cardDefined.From.Label, Is.EqualTo("ltb:creature:to-graveyard:controlled"));
    Assert.That(cardDefined.To.Label, Is.EqualTo("emit:token:artifact:treasure:controlled"));
    Assert.That(cardDefined.Tier, Is.EqualTo(CertaintyTier.Green));
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
}
