namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// The second reconstruction gold on the new port model (S3b, porting <c>BloodArtistEngineTest</c>) —
/// a <b>different</b> combo than Chatterfang × Pitiless, proving the walk + engine generalize. Ruthless
/// Knave sacrifices a creature for Treasure; Blood Artist drains on <em>any</em> creature's death. The
/// sac→death hop discriminates within one gold: the creature-sac hop is <b>GREEN</b>
/// (<c>creature ⊆ creature</c>, <c>you-control ⊆ any</c>) — the sound contrast to gold 1's irreducible
/// <c>Squirrel ⊄ creature</c> AMBER — while Ruthless Knave's <em>Treasure</em>-sac hop is a sound
/// negative (a Treasure is provably not a creature → AMBER "Types"). Tier reaches GREEN; gold 1's
/// AMBER is the straddle, not a ceiling.
/// <para>ADR-0003 §5: the hop is now the death EMIT the sac raises (<c>emit:removal:…:sacrificed</c>) →
/// the dies consume (<c>ltb:…:to-graveyard</c>), matched by subsumption — not the retired consume→consume
/// bridge. The emit carries the sac's fodder as its Subject, so the operator tiers on the identical
/// (fodder, dying) pair the bridge did — the GREEN/AMBER verdicts are unchanged.</para>
/// </summary>
[TestFixture]
public class BloodArtistPortGraphTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  /// <summary>Project a card from its parse gold. These used to read a private copy under
  /// <c>Fixtures/Interactions/cards/</c>; those four ASTs were byte-equivalent (modulo the SourceSpan /
  /// OracleLineIndex provenance the parse golds additionally carry) to their <c>HandParsedCards</c>
  /// twins, so the duplicate was deleted and this reads the single hand-parsed gold.</summary>
  private static PortGraph Walk(string relativePath, string card)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      Path.Combine(relativePath.Split('/'))
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    return new PortWalk(Ontology).Project(card, gold!["Output"]!["Oracle"]!["Abilities"]);
  }

  private static IReadOnlyList<PortEdge> Edges() =>
    new PortGraphEngine(Ontology).Materialize(
      new[]
      {
        Walk("HandParsedCards/BloodArtist.json", "Blood Artist"),
        Walk("HandParsedCards/XLN/RuthlessKnave.json", "Ruthless Knave"),
      }
    );

  // Creature-sac → "a creature dies": every you-controlled creature IS a creature → reliable GREEN.
  [Test]
  public void Creature_sac_to_creature_death_is_green()
  {
    var edge = Edges()
      .Single(e =>
        e.From.Label == "emit:removal:creature:to-graveyard:sacrificed:controlled"
        && e.To.Label == "ltb:creature:to-graveyard"
      );
    Assert.That(edge.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(edge.Reliability, Is.EqualTo(Trilean.Yes));
    Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Green));
    Assert.That(edge.Reason, Is.Null);
  }

  // Treasure-sac → "a creature dies": a Treasure is provably an artifact, not a creature — the hop
  // survives the Intersects prune (Overlaps) but the operator returns a definitive No. Sound negative.
  [Test]
  public void Treasure_sac_to_creature_death_is_amber_types()
  {
    var edge = Edges()
      .Single(e =>
        e.From.Label == "emit:removal:artifact:treasure:to-graveyard:sacrificed:controlled"
        && e.To.Label == "ltb:creature:to-graveyard"
      );
    Assert.That(edge.Overlap, Is.EqualTo(FilterRelation.Overlaps));
    Assert.That(edge.Reliability, Is.EqualTo(Trilean.No));
    Assert.That(edge.Tier, Is.EqualTo(CertaintyTier.Amber));
    Assert.That(edge.Reason, Is.EqualTo("Types"));
  }
}
