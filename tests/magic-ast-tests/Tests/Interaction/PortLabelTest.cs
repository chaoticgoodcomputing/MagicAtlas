namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR-0002 §1–3 conformance: <see cref="PortLabel"/> projects an AST sub-tree to its canonical
/// colon-label deterministically. {filter (+ role) → expected label}, mirroring the operator's
/// FilterRelations conformance fixtures. S1 scope: the two consume-port roles the
/// Chatterfang × Pitiless gold needs (a "dies" trigger and a sacrifice cost), now with the
/// type-ontology subject-lift (subtype → permanent card-type).
/// </summary>
[TestFixture]
public class PortLabelTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  // --- tracer: a "dies" trigger you control (Pitiless's shape) ---
  [Test]
  public void Death_trigger_projects_ltb_to_graveyard_with_scope() =>
    Assert.That(
      PortLabel.DeathTrigger(
        new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
        Ontology
      ),
      Is.EqualTo("ltb:creature:to-graveyard:controlled")
    );

  [Test]
  public void Death_trigger_any_controller_drops_scope_to_broadest_prefix() =>
    Assert.That(
      PortLabel.DeathTrigger(new ObjectFilter { CardTypes = ["creature"] }, Ontology),
      Is.EqualTo("ltb:creature:to-graveyard")
    );

  // The real Pitiless: "Whenever another creature you control dies" — exclude-self carried.
  [Test]
  public void Death_trigger_another_creature_you_control_carries_exclusion() =>
    Assert.That(
      PortLabel.DeathTrigger(
        new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
          ExcludeSelf = true,
        },
        Ontology
      ),
      Is.EqualTo("ltb:creature:to-graveyard:controlled:another")
    );

  // --- sacrifice cost (Chatterfang) — subtype-only fodder lifted to its permanent card-type ---
  [Test]
  public void Sacrifice_cost_lifts_squirrel_subtype_to_creature_and_floors_to_controlled() =>
    Assert.That(
      PortLabel.SacrificeCost(new ObjectFilter { Subtypes = ["Squirrel"] }, Ontology),
      Is.EqualTo("sac:creature:squirrel:controlled")
    );

  [Test]
  public void Sacrifice_cost_creature_fodder() =>
    Assert.That(
      PortLabel.SacrificeCost(new ObjectFilter { CardTypes = ["creature"] }, Ontology),
      Is.EqualTo("sac:creature:controlled")
    );

  // The lift drops kindred (Squirrel → {creature, kindred}); kindred is not a permanent type, so a
  // sac/death port — which acts on a permanent — resolves to creature alone. Treasure → artifact.
  [Test]
  public void Subject_lifts_subtype_to_permanent_card_type_only()
  {
    Assert.That(
      PortLabel.Subject(new ObjectFilter { Subtypes = ["Squirrel"] }, Ontology),
      Is.EqualTo("creature:squirrel")
    );
    Assert.That(
      PortLabel.Subject(new ObjectFilter { Subtypes = ["Treasure"] }, Ontology),
      Is.EqualTo("artifact:treasure")
    );
  }

  // Determinism: a multi-type filter canonicalises (sorted, lower-cased) so the leaf is stable.
  [Test]
  public void Subject_canonicalises_multi_type_deterministically() =>
    Assert.That(
      PortLabel.Subject(new ObjectFilter { CardTypes = ["Creature", "artifact"] }, Ontology),
      Is.EqualTo("artifact+creature")
    );

  // --- projection over the REAL parsed Chatterfang gold (not a hand-built filter) ---
  [Test]
  public void Projects_chatterfangs_real_sacrifice_cost()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      "MH2",
      "Chatterfang.json"
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    var abilities = (JsonArray)gold!["Output"]!["Oracle"]!["Abilities"]!;

    JsonNode? fodder = null;
    foreach (var ability in abilities)
      foreach (var cost in ability?["Costs"] as JsonArray ?? [])
        if (cost?["CostType"]?.ToString() == "sacrifice")
          fodder = cost["Filter"];

    Assert.That(fodder, Is.Not.Null, "Chatterfang should have a sacrifice cost");
    var filter = fodder.Deserialize<ObjectFilter>(MagicAST.MagicASTJsonOptions.Strict)!;
    Assert.That(
      PortLabel.SacrificeCost(filter, Ontology),
      Is.EqualTo("sac:creature:squirrel:controlled")
    );
  }
}
