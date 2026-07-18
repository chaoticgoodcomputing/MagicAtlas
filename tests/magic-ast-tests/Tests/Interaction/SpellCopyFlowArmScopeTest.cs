namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Spell-copy flow arm — SCOPE PINS (see <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c> and the
/// "SEPARATE arm" note in <c>copy-inheritance-scope.md §7</c>). A spell-copy ("copy target instant or
/// sorcery spell" — Dualcaster Mage's ETB, Reiterate, Narset's Reversal) puts a copy of a spell ON THE
/// STACK. CR 707.10: <b>a copy of a spell isn't cast</b> — it reproduces the copied spell's
/// characteristics, modes, targets, and X, but it does NOT trigger "whenever you cast a spell."
///
/// <para><b>What landed (parse-layer prerequisite):</b> the spell-copy now projects a distinct, faithful
/// <c>emit:copy:spell</c> label (NON-NULL Subject = the copied-spell filter), separate from a permanent
/// token-copy's <c>emit:copy</c> (Kiki / Cackling Counterpart's "create a token that's a copy of a
/// creature"). This keeps the copy-inheritance PERMANENT graft (which keys on the bare <c>emit:copy</c>)
/// from ever grafting a stack spell-copy as a permanent — a spell on the stack has no ETB/untap to graft.</para>
///
/// <para><b>The arm (LANDED 2026-06-18, interaction-judge PROCEED):</b> the sound refuel is the copy
/// <em>reproducing the copied spell's effects</em> — the <c>("copy","cast")</c> arm
/// (<c>PortGraphEngine.SpellCopyReFiresEffects</c>) feeds a type-compatible spell's <c>cast:spell:self</c>
/// effect-driver, NOT an <c>emit:copy:spell → trigger:cast</c> (CR 707.10 makes that unsound; the arm's
/// Role≠trigger keying makes it structurally impossible). All combos reconstruct at honest <b>AMBER</b>
/// (the IsSelf Subsumes=No floor); the live tier pins are below.</para>
/// </summary>
[TestFixture]
public class SpellCopyFlowArmScopeTest
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
    return new PortWalk(Ontology).Project(card, gold!["Output"]!["Oracle"]!["Abilities"]);
  }

  /// <summary>
  /// FAITHFUL PROJECTION. Dualcaster Mage's ETB "copy target instant or sorcery spell"
  /// (<c>Target.Zone:Stack</c>) must project the distinct <c>emit:copy:spell</c> label — NOT the bare
  /// <c>emit:copy</c> a permanent token-copy uses — carrying the copied-spell filter as a NON-NULL Subject
  /// (adding-a-flow-arm anti-pattern 3). This is the discriminator a future spell-copy arm tiers on.
  /// </summary>
  [Test]
  public void Dualcaster_spell_copy_projects_a_distinct_emit_copy_spell_label_with_a_non_null_subject()
  {
    var graph = Walk("", "DualcasterMage.json", "Dualcaster Mage");

    var spellCopy = graph.Ports.SingleOrDefault(p =>
      p.Side == PortSide.Emit && p.Label.StartsWith("emit:copy:spell", StringComparison.Ordinal)
    );

    Assert.That(
      spellCopy,
      Is.Not.Null,
      "a 'copy target instant or sorcery spell' (Zone:Stack) must project emit:copy:spell, not emit:copy"
    );
    Assert.That(
      graph.Ports.Any(p => p.Label == "emit:copy"),
      Is.False,
      "a spell-copy must NOT project the bare emit:copy (the permanent token-copy graft label)"
    );
    Assert.That(spellCopy!.Subject, Is.Not.Null, "the copied-spell filter must ride as a NON-NULL Subject");
    Assert.That(
      spellCopy.Subject!.Zone,
      Is.EqualTo(Zone.Stack),
      "the Subject is the on-stack spell filter (the spell-copy discriminator)"
    );
  }

  /// <summary>
  /// PERMANENT token-copy stays on the <c>emit:copy</c> path. Cackling Counterpart's "create a token
  /// that's a copy of target creature you control" (no <c>Zone:Stack</c>) is a permanent copy — it must
  /// keep the bare <c>emit:copy</c> label so the copy-inheritance graft still reads it. The
  /// <c>:spell</c> facet must distinguish ONLY the stack spell-copy, never a permanent copy.
  /// </summary>
  [Test]
  public void Cackling_counterpart_permanent_token_copy_keeps_the_bare_emit_copy_label()
  {
    var graph = Walk("ISD", "CacklingCounterpart.json", "Cackling Counterpart");

    Assert.That(
      graph.Ports.Any(p => p.Label == "emit:copy"),
      Is.True,
      "a permanent token-copy (no Zone:Stack) keeps emit:copy for the copy-inheritance graft"
    );
    Assert.That(
      graph.Ports.Any(p => p.Label.StartsWith("emit:copy:spell", StringComparison.Ordinal)),
      Is.False,
      "a permanent copy is NOT a spell-copy — it must not project emit:copy:spell"
    );
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD. A spell-copy must never be grafted as a PERMANENT. Dualcaster Mage
  /// (emit:copy:spell, Subject Zone:Stack) paired with a creature must NOT manufacture a copy-graft of
  /// that creature onto a copy identity (a spell on the stack has no battlefield ports). The
  /// copy-inheritance graft keys on the bare emit:copy, so emit:copy:spell is excluded — no grafted copy
  /// identity, no spurious cycle. (Reiterate stands in as the second card: it too only spell-copies, so
  /// no permanent graft can form, and these two share no closing arm — the pair stays Missed.)
  /// </summary>
  [Test]
  public void Spell_copy_is_never_grafted_as_a_permanent_the_false_positive_guard()
  {
    var graphs = new[]
    {
      Walk("", "DualcasterMage.json", "Dualcaster Mage"),
      Walk("", "Reiterate.json", "Reiterate"),
    };
    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(graphs);

    Assert.That(
      edges.Any(e => e.From.Grafter is not null || e.To.Grafter is not null
        || e.From.CopiedFrom is not null || e.To.CopiedFrom is not null),
      Is.False,
      "a stack spell-copy must not produce any grafted (Grafter/CopiedFrom) copy identity"
    );
    Assert.That(
      engine.FindCycles(edges).Any(),
      Is.False,
      "two pure spell-copies share no closing arm yet — no cycle may be manufactured"
    );
  }

  // ----- spell-copy → cast arm (LANDED 2026-06-18, interaction-judge PROCEED): live tier pins -----

  /// <summary>bench 147-1810 (Dualcaster Mage + Cackling Counterpart). Cackling copies Dualcaster (a token
  /// Dualcaster); the token's ETB copies the Cackling Counterpart spell, re-running it to make another
  /// token Dualcaster. The ("copy","cast") arm re-fires the copied spell's effects → <b>AMBER</b>: the
  /// copied "instant or sorcery" only Intersects (≠ Subsumes) the recast spell's self-type (IsSelf
  /// Subsumes=No floor, CR 707.10) — honest, never a false GREEN.</summary>
  [Test]
  public void Dualcaster_x_cackling_counterpart_reconstructs_amber()
  {
    var graphs = new[]
    {
      Walk("", "DualcasterMage.json", "Dualcaster Mage"),
      Walk("ISD", "CacklingCounterpart.json", "Cackling Counterpart"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Label.StartsWith("emit:copy:spell", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:copy:spell", StringComparison.Ordinal))
    );
    Assert.That(loop, Is.Not.Null, "the spell-copy must reproduce Cackling Counterpart, refueling the loop");
    Assert.That(loop!.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  /// <summary>bench 147-1987 (Ghostly Flicker + Dualcaster Mage). Ghostly Flicker blinks Dualcaster;
  /// Dualcaster's ETB copies the Ghostly Flicker spell; the copy re-flickers Dualcaster, re-firing the ETB
  /// (the spell-copy reproduces a blink → the existing blink→etb arm closes it). Target tier: <b>AMBER</b>
  /// — <em>irreducibly</em> so, via TWO stacked identity-Subsumes=No floors, not one: (1) the
  /// <c>("blink","etb")</c> edge (<see cref="BlinkSatisfiesEnter"/>) can't prove Ghostly Flicker's broad
  /// "two target artifacts/creatures/lands" blink Subsumes Dualcaster's IsSelf ETB watch, and (2) the
  /// <c>("copy","cast")</c> edge (<c>bridge:spell-copy-to-cast-driver</c>, declared <c>ceiling: AMBER</c> in
  /// the <c>dualcaster-mage-x-ghostly-flicker</c> gold) can't prove the copied "instant or sorcery" Subsumes
  /// Ghostly Flicker's own IsSelf cast:spell:self driver. Both edges float on the caster's free choice of
  /// legal target — a choice the AST cannot statically prove selects THIS card — so the loop is capped
  /// AMBER by construction, not by a temporary gap. Audited 2026-07-18 (Currency-B precision-fix pass):
  /// confirmed genuinely AMBER, matches the pin in <c>combo-expected-tiers.json</c>; no GREEN is reachable
  /// through this 2-card cycle.</summary>
  [Test]
  public void Ghostly_flicker_x_dualcaster_reconstructs_amber_the_double_identity_floor()
  {
    var graphs = new[]
    {
      Walk("", "GhostlyFlicker.json", "Ghostly Flicker"),
      Walk("", "DualcasterMage.json", "Dualcaster Mage"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));
    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e =>
        e.From.Card.Contains("Dualcaster", StringComparison.Ordinal)
        || e.To.Card.Contains("Dualcaster", StringComparison.Ordinal))
    );
    Assert.That(loop, Is.Not.Null, "the blink→etb→copy→cast round-trip must close a cycle");
    Assert.That(loop!.Tier, Is.EqualTo(CertaintyTier.Amber));
  }

  /// <summary>bench 11-3368 (Narset's Reversal + Reiterate). Each copies the other's spell; Reiterate's
  /// buyback returns it to hand to recast, and Narset's Reversal returns the copied spell to hand — the
  /// copy-of-spell graft must reproduce a spell-copy that re-copies. Target tier: <b>AMBER</b> — Reiterate's
  /// buyback is a {3} mana co-cost the loop must cover (§8 balance), so the loop is mana-negative without an
  /// external mana source and floors honestly. (Pins the §8 co-cost discipline for the deferred arm.)</summary>
  [Test]
  public void Narsets_reversal_x_reiterate_reconstructs_amber_buyback_is_a_mana_co_cost()
  {
    var graphs = new[]
    {
      Walk("", "NarsetsReversal.json", "Narset's Reversal"),
      Walk("", "Reiterate.json", "Reiterate"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));
    var loop = cycles.FirstOrDefault();
    Assert.That(loop, Is.Not.Null);
    Assert.That(loop!.Tier, Is.EqualTo(CertaintyTier.Amber));
  }
}
