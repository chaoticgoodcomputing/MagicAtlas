namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Damage flow arm — ACCEPTANCE PINS (see <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>).
/// A "deals N damage" effect (CR 119/120) emits a damage event; "whenever [source] deals [combat] damage
/// [to recipient]" (CR 120 general / CR 510 combat) consumes it. The arm connects
/// <c>emit:damage:&lt;combat&gt;:&lt;recipient&gt;</c> → <c>trigger:damage:…</c>, keyed on the damage SOURCE
/// Subject, with three soundness gates this fixture pins:
/// <list type="number">
///   <item><b>Combat-vs-noncombat</b> (the combat-damage-modeling blocker): a non-combat emit (CR 120) feeds
///   a general or non-combat trigger but NEVER a combat-specific trigger (CR 510) — the false GREEN the
///   blocker memory warned of.</item>
///   <item><b>Recipient class</b>: a player-recipient emit can't feed a creature-recipient trigger (CR 510.1).</item>
///   <item><b>Self-source object identity</b>: a self-watching trigger ("whenever THIS deals damage") fires
///   only for its OWN object — matched same-card-only, so a DIFFERENT card's self-source damage does not feed
///   it (Brazen Dwarf's "this creature deals 1 to each opponent" never triggers the Vehicle's Crash Land).</item>
/// </list>
/// </summary>
[TestFixture]
public class DamageFlowArmScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static readonly ObjectFilter Self = new() { IsSelf = true };
  private static readonly ObjectFilter CreatureYouControl =
    new() { CardTypes = ["creature"], Controller = ControllerFilter.You };

  private static PortNode Emit(string card, string label, ObjectFilter subject) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Emit,
      Identity = $"{card}::{label}",
      Subject = subject,
    };

  private static PortNode Consume(string card, string label, ObjectFilter subject) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Consume,
      Identity = $"{card}::{label}",
      Subject = subject,
    };

  private static IReadOnlyList<PortEdge> Edges(params PortNode[] ports) =>
    new PortGraphEngine(Ontology).Materialize([new PortGraph { Ports = ports }]);

  private static bool HasDamageHop(IReadOnlyList<PortEdge> edges) =>
    edges.Any(e =>
      e.From.Label.StartsWith("emit:damage", StringComparison.Ordinal)
      && e.To.Label.StartsWith("trigger:damage", StringComparison.Ordinal)
    );

  /// <summary>The Crash Land self-loop hop: the SAME card's own (self-source) damage refuels its own
  /// "whenever this deals damage" trigger — the arm forms the edge (the operator tiers it Green on
  /// self↔self; a §8 conditional gate would floor the full cycle to Amber, tested elsewhere).</summary>
  [Test]
  public void Self_damage_feeds_same_card_self_trigger()
  {
    var emit = Emit("Vehicle", PortLabel.DealDamageEmit(PortLabel.DamageNoncombat, "any"), Self);
    var trigger = Consume("Vehicle", PortLabel.DamageTrigger(PortLabel.DamageAnyKind, "any"), Self);
    var hop = Edges(emit, trigger).SingleOrDefault(e => HasDamageHop([e]));
    Assert.That(hop, Is.Not.Null, "a card's own damage must refuel its own 'whenever this deals damage' trigger");
  }

  /// <summary>FALSE-POSITIVE GUARD (the load-bearing soundness point). A DIFFERENT card's self-source damage
  /// must NOT feed a self-watching trigger — Brazen Dwarf's "this creature deals 1 to each opponent" never
  /// triggers the Vehicle's Crash Land "whenever THIS Vehicle deals damage".</summary>
  [Test]
  public void Other_card_self_damage_does_not_feed_self_trigger()
  {
    var brazenDwarf = Emit("Brazen Dwarf", PortLabel.DealDamageEmit(PortLabel.DamageNoncombat, "opponent"), Self);
    var crashLand = Consume("Vehicle", PortLabel.DamageTrigger(PortLabel.DamageAnyKind, "any"), Self);
    Assert.That(
      HasDamageHop(Edges(brazenDwarf, crashLand)),
      Is.False,
      "a self-watching trigger fires only for its OWN object — a different card's self-source damage must not feed it"
    );
  }

  /// <summary>COMBAT SOUNDNESS (the blocker memory). A NON-combat damage emit (CR 120) must NOT feed a
  /// combat-specific trigger (CR 510) — the exact false GREEN the combat-vs-noncombat distinction prevents.</summary>
  [Test]
  public void Noncombat_damage_does_not_feed_combat_trigger()
  {
    var emit = Emit("Burn", PortLabel.DealDamageEmit(PortLabel.DamageNoncombat, "any"), CreatureYouControl);
    var combatTrigger = Consume("Watcher", PortLabel.DamageTrigger(PortLabel.DamageCombat, "player"), CreatureYouControl);
    Assert.That(
      HasDamageHop(Edges(emit, combatTrigger)),
      Is.False,
      "non-combat damage (CR 120) must never feed a combat-damage trigger (CR 510)"
    );
  }

  /// <summary>A COMBAT damage emit DOES feed a combat-damage trigger of a compatible recipient (the arm is
  /// not vacuous — combat presence really does drive combat-damage triggers).</summary>
  [Test]
  public void Combat_damage_feeds_combat_trigger()
  {
    var emit = Emit("Attacker", PortLabel.DealDamageEmit(PortLabel.DamageCombat, "any"), CreatureYouControl);
    var combatTrigger = Consume("Watcher", PortLabel.DamageTrigger(PortLabel.DamageCombat, "player"), CreatureYouControl);
    Assert.That(
      HasDamageHop(Edges(emit, combatTrigger)),
      Is.True,
      "combat damage from a creature you control must feed a 'deals combat damage to a player' trigger"
    );
  }

  /// <summary>RECIPIENT SOUNDNESS. A player-recipient emit must NOT feed a creature-recipient trigger
  /// (CR 510.1 — combat damage to a player is not damage to a creature).</summary>
  [Test]
  public void Player_recipient_does_not_feed_creature_recipient_trigger()
  {
    var emit = Emit("Attacker", PortLabel.DealDamageEmit(PortLabel.DamageCombat, "player"), CreatureYouControl);
    var creatureTrigger = Consume("Watcher", PortLabel.DamageTrigger(PortLabel.DamageCombat, "creature"), CreatureYouControl);
    Assert.That(
      HasDamageHop(Edges(emit, creatureTrigger)),
      Is.False,
      "damage to a player must not feed a 'deals combat damage to a creature' trigger"
    );
  }

  /// <summary>FALSE-POSITIVE GUARD. A non-damage emit (a token) must not manufacture a damage edge.</summary>
  [Test]
  public void Non_damage_emit_feeds_no_damage_trigger()
  {
    var token = Emit("Token Maker", "emit:token:creature:soldier:controlled", new ObjectFilter { CardTypes = ["creature"], IsToken = true });
    var trigger = Consume("Watcher", PortLabel.DamageTrigger(PortLabel.DamageAnyKind, "any"), Self);
    Assert.That(
      Edges(token, trigger).Any(e => e.To.Label.StartsWith("trigger:damage", StringComparison.Ordinal)),
      Is.False,
      "a token emit is not damage — no edge may feed a damage trigger"
    );
  }

  /// <summary>The combat-presence projection (CR 510, decision 1): a creature card with a combat profile
  /// projects a GATED <c>emit:damage:combat:any</c> (self) — the structural combat-damage emit that has no
  /// effect to read. A 0-power creature projects none. Verified through the real <see cref="PortWalk.Project"/>.</summary>
  [Test]
  public void Combat_presence_projects_a_gated_combat_emit_for_an_attacker()
  {
    var attacker = new PortWalk(Ontology).Project(
      "Bear",
      new JsonArray(),
      cardProfile: new JsonObject { ["Types"] = new JsonArray("creature"), ["Power"] = 2 }
    );
    var emit = attacker.Ports.SingleOrDefault(p =>
      p.Label.StartsWith("emit:damage:combat", StringComparison.Ordinal)
    );
    Assert.That(emit, Is.Not.Null, "an attacking creature projects a structural combat-damage emit (CR 510)");
    Assert.That(emit!.Gated, Is.True, "combat is once-per-combat — the emit is gated so loops through it floor to Amber");
    Assert.That(emit.Subject?.IsSelf, Is.True, "the combat-damage source is the creature itself");

    var wall = new PortWalk(Ontology).Project(
      "Zero-Power Wall",
      new JsonArray(),
      cardProfile: new JsonObject { ["Types"] = new JsonArray("creature"), ["Power"] = 0 }
    );
    Assert.That(
      wall.Ports.Any(p => p.Label.StartsWith("emit:damage:combat", StringComparison.Ordinal)),
      Is.False,
      "a 0-power creature deals no combat damage — no combat-presence emit"
    );
  }
}
