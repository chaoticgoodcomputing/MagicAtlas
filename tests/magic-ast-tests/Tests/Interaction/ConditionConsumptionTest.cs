namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0004 §6 — <b>modeled-dependency completeness</b>. Gates the derivation that powers the
/// over-approximation report (the <c>OverApproximation</c> Flowthru flow): <c>AST condition nodes −
/// conditions the projection consumed</c>, with "consumed" derived by ABLATION rather than declared.
///
/// <para>The report itself is a report — corpus-gated, gitignored, never a gate (project convention:
/// diagnostics are Flowthru flows, NUnit is for gates). What IS gated here is the machinery and the
/// acceptance witness, both hermetic: the committed hand-parsed golds. Without this, the report could
/// silently start computing an empty delta and nothing would notice — the exact absence-blindness §6
/// exists to close.</para>
///
/// <para>Stateless invariants + named witnesses throughout: no count baseline, no ratchet. Every
/// assertion here names the card and the clause it is about.</para>
/// </summary>
[TestFixture]
public class ConditionConsumptionTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static JsonArray Abilities(string file)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      file
    );
    return (JsonArray)JsonNode.Parse(File.ReadAllText(path))!["Output"]!["Oracle"]!["Abilities"]!;
  }

  /// <summary>
  /// The acceptance witness the issue names. Gravecrawler's second ability is
  /// <c>alternativeCast{FromZone:Graveyard}</c> carrying a <c>count</c> Condition — "you control a
  /// Zombie" ≥ 1 — and <c>PortWalk.EmitPort</c> reads only <c>FromZone</c> and <c>Cost</c>. The
  /// <c>emit:returntobattlefield:self</c> that underwrites the aristocrat-recursion loops is therefore
  /// projected unconditionally: a real, legal over-approximation that was previously "permanently
  /// invisible to any report."
  /// </summary>
  [Test]
  public void Gravecrawlers_zombie_clause_is_a_dropped_condition()
  {
    var abilities = Abilities("Gravecrawler.json");
    var dropped = ConditionConsumption.Dropped(new PortWalk(Ontology), "Gravecrawler", abilities);

    var zombie = dropped.SingleOrDefault(d => d.Site.ConditionType == "count");
    Assert.That(zombie, Is.Not.Null, "the 'as long as you control a Zombie' count condition must be reported as dropped");
    Assert.That(zombie!.Site.Json, Does.Contain("Zombie"));
    Assert.That(zombie.Site.Path, Is.EqualTo("[1].Effects[0].Condition"));
    Assert.That(
      zombie.AffectedPortLabels,
      Does.Contain("emit:returntobattlefield:self"),
      "the recast emit rests on the unmodeled Zombie condition — that is the whole point of the report"
    );
  }

  /// <summary>
  /// The other side of the delta, so the derivation cannot pass by reporting everything as dropped.
  /// Kodama of the East Tree's second ability carries an <c>InterveningIf</c> ("if it wasn't put onto the
  /// battlefield with this ability"). <c>PortWalk.IsGated</c> reads its PRESENCE — every port of that
  /// ability is marked <see cref="PortNode.Gated"/> — so ablating it changes the projection and it is
  /// CONSUMED, never a dropped over-approximation.
  /// </summary>
  [Test]
  public void An_intervening_if_is_consumed_not_dropped()
  {
    var abilities = Abilities("KodamaoftheEastTree.json");
    var walk = new PortWalk(Ontology);

    var sites = ConditionConsumption.Collect(abilities);
    Assert.That(sites.Select(s => s.Path), Does.Contain("[1].InterveningIf"), "the condition must be FOUND");

    var dropped = ConditionConsumption.Dropped(walk, "Kodama of the East Tree", abilities);
    Assert.That(
      dropped.Select(d => d.Site.Path),
      Does.Not.Contain("[1].InterveningIf"),
      "an intervening-if raises the §8 Gated flag, so the projection demonstrably consumes it"
    );
  }

  /// <summary>
  /// The ablation itself, stated directly: removing a condition node the projection consumes must move
  /// the fingerprint, and removing one it drops must not. This is what makes "consumed" a derived fact
  /// instead of a maintained list — and it is the property that keeps the report honest as the projection
  /// grows new condition-reading slices.
  /// </summary>
  [Test]
  public void Ablation_moves_the_fingerprint_exactly_for_consumed_conditions()
  {
    var walk = new PortWalk(Ontology);

    var kodama = Abilities("KodamaoftheEastTree.json");
    var kodamaIf = ConditionConsumption.Collect(kodama).Single(s => s.Path == "[1].InterveningIf");
    Assert.That(
      ConditionConsumption.Fingerprint(walk.Project("Kodama of the East Tree", ConditionConsumption.Ablate(kodama, kodamaIf.Ordinal))),
      Is.Not.EqualTo(ConditionConsumption.Fingerprint(walk.Project("Kodama of the East Tree", kodama))),
      "consumed ⇒ the projection differs without it"
    );

    var crawler = Abilities("Gravecrawler.json");
    var zombie = ConditionConsumption.Collect(crawler).Single(s => s.ConditionType == "count");
    Assert.That(
      ConditionConsumption.Fingerprint(walk.Project("Gravecrawler", ConditionConsumption.Ablate(crawler, zombie.Ordinal))),
      Is.EqualTo(ConditionConsumption.Fingerprint(walk.Project("Gravecrawler", crawler))),
      "dropped ⇒ the projection is bit-identical without it"
    );
  }

  /// <summary>Ablation must never mutate its input — the report re-projects the same AST many times.</summary>
  [Test]
  public void Ablation_does_not_mutate_the_source_ast()
  {
    var abilities = Abilities("Gravecrawler.json");
    var before = abilities.ToJsonString();
    var site = ConditionConsumption.Collect(abilities).Single(s => s.ConditionType == "count");
    _ = ConditionConsumption.Ablate(abilities, site.Ordinal);
    Assert.That(abilities.ToJsonString(), Is.EqualTo(before));
  }
}
