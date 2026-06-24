namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Scope pins for the extra-combat flow arm (CR 500.8): an additional combat phase
/// (<c>emit:additionalcombat</c> — Aggravated Assault, Breath of Fury, Combat Celebrant) lets a
/// creature attack AGAIN, satisfying an <c>attacksorblocks</c> consume. This re-drives a creature's
/// combat-damage emit (closing the Breath of Fury / Aggravated Assault infinite-combat loop) and
/// re-fires a card's own "whenever this attacks" roll trigger (the dice offshoot). The combat-damage
/// emit stays Gated so any loop floors to AMBER — never a false GREEN.
/// </summary>
[TestFixture]
public class ExtraCombatArmScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static readonly ObjectFilter Self = new() { IsSelf = true };
  private static readonly ObjectFilter YouControl = new() { Controller = ControllerFilter.You };

  private static PortNode Emit(string card, string label, ObjectFilter subject) =>
    new() { Card = card, Label = label, Side = PortSide.Emit, Identity = $"{card}::{label}", Subject = subject };

  private static PortNode Consume(string card, string label, ObjectFilter subject) =>
    new() { Card = card, Label = label, Side = PortSide.Consume, Identity = $"{card}::{label}", Subject = subject };

  private static IReadOnlyList<PortEdge> Edges(params PortNode[] ports) =>
    new PortGraphEngine(Ontology).Materialize([new PortGraph { Ports = ports }]);

  private static bool HasExtraCombatHop(IReadOnlyList<PortEdge> edges) =>
    edges.Any(e =>
      e.From.Label.StartsWith("emit:additionalcombat", StringComparison.Ordinal)
      && e.To.Label.StartsWith("attacksorblocks", StringComparison.Ordinal)
    );

  /// <summary>The arm: an additional combat phase re-fires a creature's attack trigger (Velukan Dragon's
  /// "whenever this attacks or blocks, roll" — the dice offshoot). Tiered AMBER, never GREEN: re-attacking is
  /// a CHOICE (CR 508.1a "if any"), never guaranteed. The subject is what the coarse AttacksOrBlocks trigger
  /// actually projects ({creature, IsSelf}) — the regression guard for the null-subject false-GREEN.</summary>
  [Test]
  public void Additional_combat_feeds_an_attacks_roll_trigger_at_amber()
  {
    var extraCombat = Emit("Breath of Fury", PortLabel.AdditionalCombatEmit(), YouControl);
    var attackRoll = Consume(
      "Velukan Dragon",
      "attacksorblocks:creature",
      new ObjectFilter { CardTypes = ["creature"], IsSelf = true } // what PortGraph.Trigger's coarse path projects
    );
    var hop = Edges(extraCombat, attackRoll).SingleOrDefault(e =>
      e.From.Label.StartsWith("emit:additionalcombat", StringComparison.Ordinal)
    );
    Assert.That(hop, Is.Not.Null, "an additional combat phase must re-fire a creature's attacks trigger");
    Assert.That(
      hop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "re-attacking is a choice (CR 508.1a) — the hop must be AMBER, never a null-default GREEN"
    );
  }

  /// <summary>The combat-presence re-attack consume (PortLabel.AttacksConsume) is also satisfied — the hop
  /// that re-drives a creature's combat-damage emit, closing the infinite-combat loop.</summary>
  [Test]
  public void Additional_combat_feeds_combat_presence_reattack()
  {
    var extraCombat = Emit("Aggravated Assault", PortLabel.AdditionalCombatEmit(), YouControl);
    var reAttack = Consume("Velukan Dragon", PortLabel.AttacksConsume(), Self);
    Assert.That(HasExtraCombatHop(Edges(extraCombat, reAttack)), Is.True);
  }

  /// <summary>FALSE-POSITIVE GUARD: an additional combat phase is NOT combat damage — it must never feed a
  /// "whenever [a creature] deals combat damage" trigger (that hop belongs to the damage arm).</summary>
  [Test]
  public void Additional_combat_does_not_feed_a_damage_trigger()
  {
    var extraCombat = Emit("Breath of Fury", PortLabel.AdditionalCombatEmit(), YouControl);
    var damageTrigger = Consume("Watcher", PortLabel.DamageTrigger(PortLabel.DamageCombat, "player"), Self);
    var edges = Edges(extraCombat, damageTrigger);
    Assert.That(
      edges.Any(e => e.From.Label.StartsWith("emit:additionalcombat", StringComparison.Ordinal)),
      Is.False,
      "an additional combat phase is not combat damage — it must not feed a deals-combat-damage trigger"
    );
  }
}
