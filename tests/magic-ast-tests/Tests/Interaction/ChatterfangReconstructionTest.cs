namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Schema;

/// <summary>
/// The canonical mast-interaction reconstruction gold (ADR-0001): the Chatterfang, Squirrel General
/// × Pitiless Plunderer free loop, reconstructed by <b>auto-projecting ports from the real parsed
/// card ASTs</b> (the derived layer) and expanding the <b>authored JSON family-edge grammar</b>
/// (the source of truth) through the MAST-owned <c>ObjectFilter</c> relation operators.
///
///   sac-outlet --(Death: a sacrificed Squirrel dies)--> death-payoff
///   death-payoff --(Token: creates a Treasure)--> token-doubler   (modifier: doubler intercepts it)
///   token-doubler --(Token: adds a Squirrel)--> sac-outlet         (refuels the fodder)
/// </summary>
[TestFixture]
public class ChatterfangReconstructionTest
{
  private static readonly AstSchema Schema = SchemaExport.Build();
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(DataPath("type-ontology.json"))
  )!;
  private static readonly IReadOnlyList<FamilyEdge> Grammar = FamilyGrammar.Load(
    DataPath("families.json")
  );

  private static string DataPath(params string[] parts) =>
    Path.Combine([TestContext.CurrentContext.TestDirectory, "Data", "Interactions", .. parts]);

  private static IReadOnlyList<Port> Project(string file, string card)
  {
    var gold = JsonNode.Parse(File.ReadAllText(DataPath("cards", file)))!;
    var abilities = gold["Output"]!["Oracle"]!["Abilities"];
    return new PortProjector(Schema).Project(card, abilities);
  }

  [Test]
  public void Projects_the_real_golds_into_the_expected_ports()
  {
    var pitiless = Project("PitilessPlunderer.json", "Pitiless Plunderer");
    var chatterfang = Project("Chatterfang.json", "Chatterfang");

    var deathPayoff = pitiless.Single(p => p.Label == "death-payoff");
    Assert.That(deathPayoff.Consumes.Any(r => r.Kind == ResourceKind.Death), Is.True);
    Assert.That(deathPayoff.Emits.Any(r => r.Kind == ResourceKind.Token), Is.True);

    Assert.That(chatterfang.Any(p => p.Label == "sac-outlet"), Is.True);
    Assert.That(chatterfang.Any(p => p.Label == "token-doubler"), Is.True);

    // The captured death-trigger subject filter is the real {creature, You} from the parsed AST.
    var dying = deathPayoff.Consumes.First(r => r.Kind == ResourceKind.Death).Subject!;
    Assert.That(dying.CardTypes, Does.Contain("creature"));
    Assert.That(dying.Controller, Is.EqualTo(ControllerFilter.You));

    // The doubler's emitted token translates to a Squirrel token you control — the createToken's
    // Player is You, and CR 111.2 makes its creator its controller, so the projector stamps it.
    var doubler = chatterfang.Single(p => p.Label == "token-doubler");
    var squirrel = doubler.Emits.First(r => r.Kind == ResourceKind.Token).Subject!;
    Assert.That(squirrel.Subtypes, Does.Contain("Squirrel"));
    Assert.That(squirrel.IsToken, Is.True);
    Assert.That(squirrel.Controller, Is.EqualTo(ControllerFilter.You));
  }

  [Test]
  public void Reconstructs_the_free_loop_from_the_real_golds_as_amber()
  {
    var ports = Project("PitilessPlunderer.json", "Pitiless Plunderer")
      .Concat(Project("Chatterfang.json", "Chatterfang"))
      .ToList();

    var engine = new InteractionEngine(Ontology);
    var edges = engine.Materialize(ports, Grammar);
    var loop = engine.FindCycles(edges).FirstOrDefault(c => c.Edges.Count == 3);

    Assert.That(loop, Is.Not.Null, "the free loop should reconstruct from the parsed golds");
    Assert.That(loop!.Edges.Select(e => e.From.Label).Distinct().Count(), Is.EqualTo(3));

    // The loop reconstructs but is AMBER — and this is the *correct, sound* verdict, not a defect.
    // C2 resolved the sacrifice-side Controller gap (CR 701.16: you sacrifice your own), so the
    // residual is the **irreducible** Squirrel ⊄ creature straddle on the death hop: a dying
    // Squirrel isn't provably a creature (it could be a Kindred-Squirrel non-creature, CR 308.1),
    // so the operator can't certify "every sacrificed Squirrel satisfies a creature-death trigger".
    // GREEN here would be unsound; AMBER-with-attribution ("Types") is right.
    Assert.That(loop.Tier, Is.EqualTo(CertaintyTier.Amber));

    var deathHop = loop.Edges.Single(e =>
      e.From.Label == "sac-outlet" && e.To.Label == "death-payoff"
    );
    Assert.That(deathHop.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(deathHop.Reliability, Is.EqualTo(Trilean.Unknown));
    Assert.That(deathHop.Reason, Is.EqualTo("Types"));

    // The refuel hop is now GREEN: the doubler creates its Squirrels under your control (CR 111.2),
    // so the projector stamps Controller=You on the emitted token and the join proves reliability
    // (a creature Squirrel token you control ⊆ a Squirrel you control). The death-hop straddle above
    // is the sole, sound reason the cycle stays AMBER — edge C is no longer a limiter.
    var refuelHop = loop.Edges.Single(e =>
      e.From.Label == "token-doubler" && e.To.Label == "sac-outlet"
    );
    Assert.That(refuelHop.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(refuelHop.Reliability, Is.EqualTo(Trilean.Yes));
    Assert.That(refuelHop.Tier, Is.EqualTo(CertaintyTier.Green));
  }
}
