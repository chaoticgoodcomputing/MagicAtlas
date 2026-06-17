namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Displacer Kitten cast-trigger blink flow arm — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>). Displacer Kitten reads "Whenever you cast a
/// noncreature spell, exile up to one target nonland permanent you control, then return that card to the
/// battlefield" — a SpellCast trigger driving a BLINK (the exile-then-return-self composite). The loop:
/// <list type="number">
/// <item>cast a cheap noncreature spell (the aura Mourning {1}{B} / Conviction {1}{W}) →</item>
/// <item>Displacer Kitten's SpellCast trigger fires → its blink exiles &amp; returns Peregrine Drake →</item>
/// <item>Peregrine Drake re-enters → its ETB untaps up to five lands → mana →</item>
/// <item>the aura's <c>{B}/{W}: Return this Aura to its owner's hand</c> bounces it; recast it (a
/// noncreature spell) → back to the SpellCast trigger.</item>
/// </list>
///
/// <para><b>THE NEW ARM this worker lands:</b> the cast-recursion <c>(cast, trigger)</c> arm — the aura's
/// recast (an <c>emit:cast</c>, projected from its self-bounce <c>returnToHand</c> + the card's own mana
/// cost as a <c>pay:mana</c> co-cost) feeds Displacer Kitten's <c>trigger:cast</c> (projected from the
/// SpellCast trigger). The blink→etb hop (Displacer Kitten's blink re-firing Peregrine's ETB) is the
/// EXISTING blink arm.</para>
///
/// <para><b>SIBLING DEPENDENCY — the untap-lands→mana enabler.</b> The hop from Peregrine's
/// <c>emit:untap</c>(land) to the aura's recast <c>pay:mana</c> — untapped lands tap for mana — is the
/// SHARED <c>(untap-lands → pay:mana)</c> arm a sibling worker lands (the prompt's "same sibling-dependency
/// note re: the untap-lands→mana enabler"; bench family "mana-untap blink"). It is NOT this worker's arm,
/// so it is not present in this isolated worktree. The full-loop pin below therefore supplies that one
/// enabler edge as a test-local stand-in (tiered AMBER, exactly as the sibling arm does) so the closed
/// cycle can be asserted here; the orchestrator confirms the real combo-flip on the merged tree via the
/// bench.</para>
///
/// <para><b>Target tier: AMBER.</b> Three soundly-irreducible §8/operator floors apply: (i) the
/// untap-lands→mana enabler is AMBER (the external lands' colours/count are unknown — CR 107.4); (ii) the
/// blink's "nonland permanent" filter only Intersects (does not Subsume) Peregrine's "this creature" ETB;
/// (iii) the recast's broad "a spell" Subject only Intersects the trigger's "noncreature spell" filter.
/// AMBER is the honest tier, never a fudged GREEN.</para>
/// </summary>
[TestFixture]
public class DisplacerKittenCastBlinkScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  /// <summary>Walk a gold's abilities, threading its mana-cost symbols (so a recast pay:mana co-cost
  /// attaches, mirroring the bench's <c>ManaCostSymbolsFor</c>).</summary>
  private static PortGraph Walk(string file, string card)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      file
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    var manaCost = (gold!["Output"]!["Attributes"] as JsonArray)
      ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost");
    return new PortWalk(Ontology).Project(card, gold["Output"]!["Oracle"]!["Abilities"], manaCost?["Symbols"]);
  }

  /// <summary>The sibling untap-lands→mana enabler edge (an "untap N lands" emit feeds a pay:mana cost —
  /// the untapped lands tap for mana). Tiered AMBER exactly as the sibling arm does (the external lands'
  /// colours/count are unknown, CR 107.4). Supplied here as a test-local stand-in for the SHARED arm not
  /// present in this worktree, so the closed cast-blink cycle can be asserted in isolation.</summary>
  private static IReadOnlyList<PortEdge> WithUntapLandsManaEnabler(IReadOnlyList<PortEdge> edges)
  {
    var ports = edges.SelectMany(e => new[] { e.From, e.To })
      .GroupBy(p => p.Identity)
      .Select(g => g.First())
      .ToList();
    var landUntaps = ports.Where(p =>
      p.Side == PortSide.Emit
      && p.Label.StartsWith("emit:untap", StringComparison.Ordinal)
      && p.Label != "emit:untap:self"
      && p.Subject?.CardTypes is { } types
      && types.Any(t => string.Equals(t, "land", StringComparison.OrdinalIgnoreCase))
    );
    var payManas = ports.Where(p => p.Side == PortSide.Consume && p.Label.StartsWith("pay:mana", StringComparison.Ordinal));

    var augmented = edges.ToList();
    foreach (var untap in landUntaps)
      foreach (var pay in payManas)
        augmented.Add(new PortEdge
        {
          From = untap,
          To = pay,
          Provenance = EdgeProvenance.RulesDefined,
          Family = EdgeFamily.Flow,
          Overlap = FilterRelation.Overlaps,
          Reliability = Trilean.Unknown, // AMBER — external lands' colours/count unknown (sibling arm)
          Reason = "untap-lands→mana enabler (sibling arm) — external lands' colours/count unknown",
        });
    return augmented;
  }

  private static PortCycle? KittenLoop(string auraFile, string aura)
  {
    var graphs = new[]
    {
      Walk("DisplacerKitten.json", "Displacer Kitten"),
      Walk("PeregrineDrake.json", "Peregrine Drake"),
      Walk(auraFile, aura),
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = WithUntapLandsManaEnabler(engine.Materialize(graphs));
    // Bound to the product/bench reconstruction reach (6), not the unbounded default. This is the
    // 6-hop cast-blink loop that motivated raising the reach 5→6; at the old bound 5 it was truncated,
    // which is exactly why an unbounded scope test gave a false will-flip (adding-a-flow-arm.md
    // anti-pattern 5). Mirroring the reach makes this test fail loudly if the loop ever exceeds it.
    var cycles = engine.FindCycles(edges, PortGraphEngine.DefaultReconstructionReach);

    // The combo's elementary cycle: it threads Displacer Kitten's cast emit→trigger (this worker's arm) AND
    // the blink that re-enters Peregrine Drake (so it is genuinely the cast-blink loop, not a sub-cycle).
    return cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Displacer", StringComparison.Ordinal))
      && c.Edges.Any(e => e.To.Card.Contains("Peregrine", StringComparison.Ordinal))
      && c.Edges.Any(e => e.From.Label.StartsWith("emit:cast", StringComparison.Ordinal))
      && c.Edges.Any(e => e.To.Label.StartsWith("trigger:cast", StringComparison.Ordinal))
    );
  }

  /// <summary>FAITHFUL PROJECTION. Displacer Kitten's SpellCast trigger projects a <c>trigger:cast</c>
  /// consume carrying the watched (noncreature) spell filter as a NON-NULL Subject; its blink projects
  /// <c>emit:blink</c>. The aura's self-bounce <c>{mana}: Return this Aura to its owner's hand</c> projects
  /// an <c>emit:cast</c> recast (NON-NULL Subject) plus the recast <c>pay:mana</c> co-cost.</summary>
  [Test]
  public void Projects_cast_trigger_blink_and_recast_emit_with_non_null_subjects()
  {
    var kitten = Walk("DisplacerKitten.json", "Displacer Kitten");
    var castTrigger = kitten.Ports.SingleOrDefault(p =>
      p.Side == PortSide.Consume && p.Label.StartsWith("trigger:cast", StringComparison.Ordinal)
    );
    Assert.That(castTrigger, Is.Not.Null, "the SpellCast trigger must project a trigger:cast consume");
    Assert.That(castTrigger!.Subject, Is.Not.Null, "the watched-spell filter must ride as a NON-NULL Subject (anti-pattern 3)");
    Assert.That(
      kitten.Ports.Any(p => p.Side == PortSide.Emit && p.Label.StartsWith("emit:blink", StringComparison.Ordinal)),
      Is.True,
      "Displacer Kitten's exile-then-return-self composite must project emit:blink"
    );

    var aura = Walk("Mourning.json", "Mourning");
    var recast = aura.Ports.SingleOrDefault(p =>
      p.Side == PortSide.Emit && p.Label.StartsWith("emit:cast", StringComparison.Ordinal)
    );
    Assert.That(recast, Is.Not.Null, "the aura's self-bounce-to-hand must project an emit:cast recast");
    Assert.That(recast!.Subject, Is.Not.Null, "the recast spell filter must be NON-NULL (anti-pattern 3)");
    Assert.That(
      aura.Ports.Any(p => p.Side == PortSide.Consume && p.Label.StartsWith("pay:mana", StringComparison.Ordinal)),
      Is.True,
      "the recast carries the card's own mana cost as a pay:mana co-cost (§8 mana-balance floor)"
    );
  }

  /// <summary>THIS WORKER'S ARM, in isolation. The cast-recursion arm must materialise a rules-defined
  /// edge from the aura's recast <c>emit:cast</c> to Displacer Kitten's <c>trigger:cast</c> — and it must
  /// be AMBER (the broad "a spell" recast only Intersects, does not Subsume, the "noncreature spell"
  /// trigger), never a fudged GREEN. This hop holds regardless of the sibling untap-lands→mana enabler.</summary>
  [Test]
  public void Cast_recursion_arm_connects_recast_emit_to_the_spell_cast_trigger_at_amber()
  {
    var graphs = new[]
    {
      Walk("DisplacerKitten.json", "Displacer Kitten"),
      Walk("PeregrineDrake.json", "Peregrine Drake"),
      Walk("Mourning.json", "Mourning"),
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(graphs);

    var castHop = edges.SingleOrDefault(e =>
      e.From.Label.StartsWith("emit:cast", StringComparison.Ordinal)
      && e.To.Label.StartsWith("trigger:cast", StringComparison.Ordinal)
    );
    Assert.That(castHop, Is.Not.Null, "the (cast, trigger) arm must connect the aura's recast to Displacer Kitten's SpellCast trigger");
    Assert.That(
      castHop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "a broad 'a spell' recast only Intersects (not Subsumes) the 'noncreature spell' trigger → soundly AMBER, never GREEN"
    );

    // And the existing blink arm re-fires Peregrine's ETB (the cast trigger drives the blink that re-enters PD).
    Assert.That(
      edges.Any(e =>
        e.From.Label.StartsWith("emit:blink", StringComparison.Ordinal)
        && e.To.Card.Contains("Peregrine", StringComparison.Ordinal)
        && e.To.Label.StartsWith("etb", StringComparison.Ordinal)
      ),
      Is.True,
      "Displacer Kitten's blink should re-fire Peregrine Drake's ETB (existing blink→etb arm)"
    );
  }

  /// <summary>1170-3540-3821 — Displacer Kitten + Peregrine Drake + Mourning. With the sibling
  /// untap-lands→mana enabler present, the full cast-blink loop must reconstruct at AMBER.</summary>
  [Test]
  public void Displacer_kitten_x_peregrine_drake_x_mourning_reconstructs_amber()
  {
    var loop = KittenLoop("Mourning.json", "Mourning");
    Assert.That(
      loop,
      Is.Not.Null,
      "the aura recast (emit:cast) → SpellCast trigger arm, plus blink→etb and the untap→mana enabler, should close the cast-blink loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "untap-lands→mana enabler (uncertain colours/count) + Overlaps-not-Subsumes blink/cast hops → soundly AMBER, never GREEN"
    );
  }

  /// <summary>1170-3821-6058 — Displacer Kitten + Peregrine Drake + Conviction. Identical shape with the
  /// white aura; recast {1}{W}. Target: AMBER (same floors).</summary>
  [Test]
  public void Displacer_kitten_x_peregrine_drake_x_conviction_reconstructs_amber()
  {
    var loop = KittenLoop("Conviction.json", "Conviction");
    Assert.That(
      loop,
      Is.Not.Null,
      "the aura recast (emit:cast) → SpellCast trigger arm, plus blink→etb and the untap→mana enabler, should close the cast-blink loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "untap-lands→mana enabler (uncertain colours/count) + Overlaps-not-Subsumes blink/cast hops → soundly AMBER, never GREEN"
    );
  }
}
