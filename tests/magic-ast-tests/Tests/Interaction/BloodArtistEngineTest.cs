namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Schema;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// The second mast-interaction reconstruction gold (C3) — a <b>different</b> combo than Chatterfang ×
/// Pitiless, auto-projected from the real parsed corpus, proving the projector / grammar / operator
/// generalize beyond one hand-picked pair. Ruthless Knave sacrifices a creature to make Treasure;
/// Blood Artist drains on <em>any</em> creature's death.
///
/// The creature-sac → creature-death hop is <b>GREEN</b> — <c>creature ⊆ creature</c>,
/// <c>you-controlled ⊆ any controller</c> — the sound contrast to the first gold's irreducible
/// <c>Squirrel ⊄ creature</c> AMBER. It proves the tier system <em>reaches</em> GREEN: gold 1's
/// AMBER is specific to the straddle, not a system-wide ceiling.
/// </summary>
[TestFixture]
public class BloodArtistEngineTest
{
  private static readonly AstSchema Schema = SchemaExport.Build();
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;
  private static readonly IReadOnlyList<FamilyEdge> Grammar = FamilyGrammar.Load(
    DataPath("blood-artist-engine.json")
  );

  private static string DataPath(params string[] parts) =>
    Path.Combine([TestContext.CurrentContext.TestDirectory, "Fixtures", "Interactions", .. parts]);

  private static IReadOnlyList<Port> Project(string file, string card)
  {
    var gold = JsonNode.Parse(File.ReadAllText(DataPath("cards", file)))!;
    var abilities = gold["Output"]!["Oracle"]!["Abilities"];
    return new PortProjector(Schema).Project(card, abilities);
  }

  private static IReadOnlyList<InteractionEdge> Reconstruct()
  {
    var ports = Project("BloodArtist.json", "Blood Artist")
      .Concat(Project("RuthlessKnave.json", "Ruthless Knave"))
      .ToList();
    return new InteractionEngine(Ontology).Materialize(ports, Grammar);
  }

  [Test]
  public void Reconstructs_a_reliable_creature_death_handoff_as_green()
  {
    var edges = Reconstruct();

    // Ruthless Knave's creature-sac outlet emits a Death of a {creature, you-control} object;
    // Blood Artist's payoff consumes "any creature dies". Every you-controlled creature IS a
    // creature, so the handoff is reliable: Overlaps + Yes → GREEN, with no limiting Reason.
    var creatureDeath = edges.Single(e =>
      e.Resource == ResourceKind.Death
      && e.From.Emits.Any(r =>
        r.Kind == ResourceKind.Death && (r.Subject?.CardTypes?.Contains("creature") ?? false)
      )
    );
    Assert.That(creatureDeath.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(creatureDeath.Reliability, Is.EqualTo(Trilean.Yes));
    Assert.That(creatureDeath.Tier, Is.EqualTo(CertaintyTier.Green));
    Assert.That(creatureDeath.Reason, Is.Null);
  }

  [Test]
  public void Discriminates_the_treasure_sac_outlet_from_a_creature_death()
  {
    var edges = Reconstruct();

    // Ruthless Knave's *other* sac outlet sacrifices Treasures (to draw). The same grammar edge
    // pairs it with Blood Artist's creature-death payoff — but sacrificing a Treasure is NOT a
    // creature-death. The edge survives the Intersects prune (an artifact-creature Treasure is
    // admissible, so not Disjoint → Overlaps) yet the operator returns a *definitive* No: a Treasure
    // is provably not a creature, so the handoff cannot be reliable. Tier Amber, Reason "Types".
    // This is the operator discriminating within one gold — sound positive (creature) AND sound
    // negative (Treasure) — not just refusing to decide.
    var treasureDeath = edges.Single(e =>
      e.Resource == ResourceKind.Death
      && e.From.Emits.Any(r =>
        r.Kind == ResourceKind.Death && (r.Subject?.Subtypes?.Contains("Treasure") ?? false)
      )
    );
    Assert.That(treasureDeath.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(treasureDeath.Reliability, Is.EqualTo(Trilean.No));
    Assert.That(treasureDeath.Tier, Is.EqualTo(CertaintyTier.Amber));
    Assert.That(treasureDeath.Reason, Is.EqualTo("Types"));
  }
}
