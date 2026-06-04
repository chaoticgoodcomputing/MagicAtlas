namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;

/// <summary>
/// ADR-0002 §1–3 conformance: <see cref="PortLabel"/> projects an AST sub-tree to its canonical
/// colon-label deterministically. {filter (+ role) → expected label}, mirroring the operator's
/// FilterRelations conformance fixtures. S1 POC scope: the two consume-port roles the
/// Chatterfang × Pitiless gold needs (a "dies" trigger and a sacrifice cost).
/// </summary>
[TestFixture]
public class PortLabelTest
{
  // --- tracer: a "dies" trigger you control (Pitiless's shape) ---
  [Test]
  public void Death_trigger_projects_ltb_to_graveyard_with_scope() =>
    Assert.That(
      PortLabel.DeathTrigger(
        new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You }
      ),
      Is.EqualTo("ltb:creature:to-graveyard:controlled")
    );

  [Test]
  public void Death_trigger_any_controller_drops_scope_to_broadest_prefix() =>
    Assert.That(
      PortLabel.DeathTrigger(new ObjectFilter { CardTypes = ["creature"] }),
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
        }
      ),
      Is.EqualTo("ltb:creature:to-graveyard:controlled:another")
    );

  // --- sacrifice cost (Chatterfang) ---
  // Subtype-only fodder; the type-ontology lift to "creature:squirrel" is a later slice.
  [Test]
  public void Sacrifice_cost_subtype_only_floors_to_controlled_per_701_21a() =>
    Assert.That(
      PortLabel.SacrificeCost(new ObjectFilter { Subtypes = ["Squirrel"] }),
      Is.EqualTo("sac:squirrel:controlled")
    );

  [Test]
  public void Sacrifice_cost_creature_fodder() =>
    Assert.That(
      PortLabel.SacrificeCost(new ObjectFilter { CardTypes = ["creature"] }),
      Is.EqualTo("sac:creature:controlled")
    );

  // Determinism: a multi-type filter canonicalises (sorted, lower-cased) so the leaf is stable.
  [Test]
  public void Subject_canonicalises_multi_type_deterministically() =>
    Assert.That(
      PortLabel.Subject(new ObjectFilter { CardTypes = ["Creature", "artifact"] }),
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
    Assert.That(PortLabel.SacrificeCost(filter), Is.EqualTo("sac:squirrel:controlled"));
  }
}
