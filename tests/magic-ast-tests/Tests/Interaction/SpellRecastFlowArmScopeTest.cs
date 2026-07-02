namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Spell-recast flow arm — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>). The arm closes the <b>ETB-return-instant →
/// recast-spell refuel</b> hop the Blink → ETB-return-instant family turns on: an ETB that
/// <b>returns an instant or sorcery card from your graveyard to your hand</b> (Archaeomancer / Izzet
/// Chronarch) makes that spell recastable (CR 601.2 — "to cast a spell is to take it from where it is,
/// usually the hand"), and recasting it re-fires its effects. The arm projects the return as one
/// <c>emit:returntohand:spell</c> (Subject = the returned instant/sorcery filter) and connects it to a
/// <c>cast:spell:self</c> consume on the spell card (Subject = the spell's <c>{instant,sorcery}</c>
/// self-type, driving the spell's effect emits via the card-defined cast→effect edge):
/// <list type="bullet">
/// <item><b>returntohand:spell → cast</b> (the spell-recast refuel): the returned instant/sorcery is
/// recast, re-driving its on-cast effect. Ghostly Flicker (an instant) blinks Peregrine Drake +
/// Archaeomancer/Izzet Chronarch; the blinked Archaeomancer/Chronarch ETB returns Ghostly Flicker to
/// hand → it is recast → its blink re-fires.</item>
/// </list>
///
/// <para><b>This arm is the ETB-return-instant → recast hop only.</b> The combo's FULL closure also needs
/// the sibling <b>untap-lands → mana</b> enabler (Peregrine Drake untaps lands to repay Ghostly Flicker's
/// {2}{U} recast); that lands→floating-mana arm is a DISTINCT arm not modeled here (Peregrine's
/// <c>emit:untap</c> is not an <c>emit:mana</c> the §8 balance reads). So absent it, the recast's
/// {2}{U} <c>pay:mana</c> co-cost is unfed and the loop floors to a sound <b>AMBER</b> — never a false
/// GREEN (adding-a-flow-arm anti-pattern 2). The pin here is that the spell-recast arm's edge
/// (<c>emit:returntohand:spell → cast:spell:self</c>) CONNECTS and closes an elementary cycle through
/// all three combo cards, at the honest AMBER tier.</para>
/// </summary>
[TestFixture]
public class SpellRecastFlowArmScopeTest
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
      string.IsNullOrEmpty(set) ? "" : set,
      file
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    return new PortWalk(Ontology).Project(
      card,
      gold!["Output"]!["Oracle"]!["Abilities"],
      ManaCostSymbols(gold)
    );
  }

  /// <summary>The card's printed mana-cost symbols (Output.Attributes[Kind=manaCost].Symbols) — the
  /// recast's pay:mana co-cost source (Ghostly Flicker is recast for its own mana cost, {2}{U}). The bench
  /// (ComboRecallRunner) threads the same source, so this mirrors the real corpus reconstruction.</summary>
  private static JsonNode? ManaCostSymbols(JsonNode? gold) =>
    (gold?["Output"]?["Attributes"] as JsonArray)
      ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
      ?["Symbols"];

  private static PortCycle? FindRecastLoop(IReadOnlyList<PortCycle> cycles, string returnerCard) =>
    cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Ghostly Flicker", StringComparison.Ordinal))
      && c.Edges.Any(e => e.From.Card.Contains(returnerCard, StringComparison.Ordinal))
      // the spell-recast arm's edge: a spell-recursion emit feeding the cast consume.
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:returntohand:spell", StringComparison.Ordinal)
        && e.To.Label == "cast:spell:self"
      )
    );

  /// <summary>
  /// LEAD COMBO (bench 1987-2493-3821) — Ghostly Flicker + Peregrine Drake + Archaeomancer. Ghostly
  /// Flicker (instant) blinks Peregrine Drake + Archaeomancer; Archaeomancer's ETB "return target instant
  /// or sorcery card from your graveyard to your hand" returns Ghostly Flicker → recast → its blink
  /// re-fires. The spell-recast arm closes the loop: <c>Ghostly Flicker.emit:blink → Archaeomancer.etb</c>
  /// (existing blink arm) → <c>Archaeomancer.emit:returntohand:spell → Ghostly Flicker.cast:spell:self</c>
  /// (THIS arm) → <c>cast → emit:blink</c> (card-defined).
  ///
  /// <para>Target: <b>AMBER</b>. The recast's {2}{U} pay:mana co-cost is unfed (the lands→mana enabler is
  /// the sibling arm, not modeled here), so §8 floors it — a sound AMBER, never a false GREEN.</para>
  /// </summary>
  [Test]
  public void Ghostly_flicker_x_peregrine_drake_x_archaeomancer_reconstructs_amber_spell_recast()
  {
    var graphs = new[]
    {
      Walk("", "GhostlyFlicker.json", "Ghostly Flicker"),
      Walk("", "PeregrineDrake.json", "Peregrine Drake"),
      Walk("M14", "Archaeomancer.json", "Archaeomancer"),
    };
    var engine = new PortGraphEngine(Ontology);
    // Bound to the product/bench reconstruction reach, not the unbounded default (anti-pattern 5).
    var cycles = engine.FindCycles(engine.Materialize(graphs), PortGraphEngine.DefaultReconstructionReach);

    var loop = FindRecastLoop(cycles, "Archaeomancer");
    Assert.That(
      loop,
      Is.Not.Null,
      "Archaeomancer's ETB return-instant should refuel Ghostly Flicker's recast and close the loop (the spell-recast arm)"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "the recast {2}{U} co-cost is unfed (the lands→mana sibling arm is out of scope) → soundly AMBER, not GREEN"
    );
  }

  /// <summary>
  /// SECOND COMBO (bench 1987-3277-3821) — Ghostly Flicker + Peregrine Drake + Izzet Chronarch. Izzet
  /// Chronarch's ETB is identical to Archaeomancer's ("return target instant or sorcery card from your
  /// graveyard to your hand"), so the SAME spell-recast arm closes the SAME loop. Target: <b>AMBER</b>.
  /// </summary>
  [Test]
  public void Ghostly_flicker_x_peregrine_drake_x_izzet_chronarch_reconstructs_amber_spell_recast()
  {
    var graphs = new[]
    {
      Walk("", "GhostlyFlicker.json", "Ghostly Flicker"),
      Walk("", "PeregrineDrake.json", "Peregrine Drake"),
      Walk("GPT", "IzzetChronarch.json", "Izzet Chronarch"),
    };
    var engine = new PortGraphEngine(Ontology);
    // Bound to the product/bench reconstruction reach, not the unbounded default (anti-pattern 5).
    var cycles = engine.FindCycles(engine.Materialize(graphs), PortGraphEngine.DefaultReconstructionReach);

    var loop = FindRecastLoop(cycles, "Izzet Chronarch");
    Assert.That(
      loop,
      Is.Not.Null,
      "Izzet Chronarch's ETB return-instant should refuel Ghostly Flicker's recast and close the loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "the recast {2}{U} co-cost is unfed → soundly AMBER, not GREEN"
    );
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD. The spell-recast arm fires ONLY on a real spell-recursion — an instant/sorcery
  /// returned from a NON-battlefield zone. A battlefield <b>bounce</b> ("return target permanent to its
  /// owner's hand", Boomerang) must project the coarse <c>emit:returntohand</c>, NOT
  /// <c>emit:returntohand:spell</c>, so it can never refuel a cast — bouncing a permanent re-casts a
  /// creature/permanent (a re-entry), not a spell-effect re-fire.
  /// </summary>
  [Test]
  public void Battlefield_bounce_projects_coarse_returntohand_not_a_spell_recast_emit()
  {
    var graph = Walk("", "Boomerang.json", "Boomerang");
    Assert.That(
      graph.Ports.Any(p => p.Label.StartsWith("emit:returntohand:spell", StringComparison.Ordinal)),
      Is.False,
      "a battlefield bounce (return target permanent to hand) is NOT a spell-recast — no emit:returntohand:spell"
    );
    Assert.That(
      graph.Ports.Any(p =>
        p.Label.StartsWith("emit:returntohand", StringComparison.Ordinal)
        && !p.Label.StartsWith("emit:returntohand:spell", StringComparison.Ordinal)
      ),
      Is.True,
      "the bounce keeps a coarse emit:returntohand[:<bounced>] label (no flow arm reads the bounce)"
    );
  }

  /// <summary>
  /// PROJECTION PIN. Archaeomancer's ETB return-instant projects the spell-recursion emit
  /// <c>emit:returntohand:spell:instant+sorcery</c> with a NON-NULL Subject (the returned filter), driven
  /// by its self-ETB consume — the parse-layer leaf the arm reads (adding-a-flow-arm anti-pattern 3).
  /// </summary>
  [Test]
  public void Archaeomancer_etb_projects_a_nonnull_spell_recursion_emit()
  {
    var graph = Walk("M14", "Archaeomancer.json", "Archaeomancer");
    var recursion = graph.Ports.FirstOrDefault(p =>
      p.Label.StartsWith("emit:returntohand:spell", StringComparison.Ordinal)
    );
    Assert.That(recursion, Is.Not.Null, "Archaeomancer's ETB should project a spell-recursion emit");
    Assert.That(
      recursion!.Subject,
      Is.Not.Null,
      "the spell-recursion emit must carry the returned instant/sorcery filter (non-null Subject — never a scalar null-default GREEN)"
    );
  }
}
