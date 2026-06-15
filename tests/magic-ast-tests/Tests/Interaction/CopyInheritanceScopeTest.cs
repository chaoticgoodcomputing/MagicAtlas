namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Copy-token inheritance — ACCEPTANCE PINS (DESIGN ONLY; see
/// <c>libs/mast-interaction/docs/copy-inheritance-scope.md</c>). Every test is <c>[Ignore]</c>d so it
/// documents the desired tier WITHOUT running — the suite stays green. Together they define
/// "copy-inheritance done."
///
/// <para>A "create a token that's a copy of [a creature]" effect makes a token carrying the COPIED
/// card's abilities (CR 707.2). The engine today projects only a coarse <c>emit:copy</c> and never
/// grafts the copied card's port graph onto the copy, so Kiki-family loops never close. These pins fix
/// the target outcomes:</para>
/// <list type="bullet">
/// <item>Kiki + Corridor Monitor → reconstructed <b>GREEN</b> (the inherited untap is unconditional and
/// its target filter subsumes Kiki — the operator can certify the renewal; §3 + §4 of the scope doc).</item>
/// <item>Kiki + Restoration Angel → reconstructed <b>AMBER</b> (the copy grafts, but the inherited
/// blink ability is <c>optional</c> AND closes only through the blink arm, which is a SEPARATE feature
/// out of this scope).</item>
/// <item>Kiki + a vanilla creature → <b>NO cycle</b> (the false-positive guard: a copy of a creature
/// with no ability that acts back on Kiki must not manufacture a combo — §3 of the scope doc).</item>
/// </list>
/// </summary>
[TestFixture]
public class CopyInheritanceScopeTest
{
  private const string Pending = "copy-inheritance — pending";

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
  /// LEAD COMBO (bench 618-4404). Kiki taps to "create a token that's a copy of target nonlegendary
  /// creature you control" — the copy of Corridor Monitor carries Corridor's ETB "untap target artifact
  /// or creature," which untaps Kiki, refunding its {T} so it can copy again. The purest copy loop: the
  /// inherited ability is a plain unconditional untap, so no second arm (blink) is needed.
  ///
  /// <para>Target: <b>GREEN</b>. The copy's grafted ports close the loop through the generalized
  /// <c>TapGatesRenewed</c> (a target-untap whose filter <c>Subsumes</c> the tap-gated Kiki — Decision 4),
  /// and the copy filter (<c>creature / !Legendary / Controller:You</c>) <c>Subsumes</c> Corridor Monitor,
  /// so the operator certifies the graft (Decision 3). interaction-judge must PROCEED on this GREEN.</para>
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Kiki_x_corridor_monitor_reconstructs_green_via_inherited_target_untap()
  {
    var graphs = new[]
    {
      Walk("", "KikiJikiMirrorBreaker.json", "Kiki-Jiki, Mirror Breaker"),
      Walk("ELD", "CorridorMonitor.json", "Corridor Monitor"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Kiki", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:untap", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:untap", StringComparison.Ordinal)
      )
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "the copy of Corridor Monitor should graft Corridor's untap and close Kiki's tap loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Green),
      "unconditional inherited untap + copy-filter Subsumes Corridor — the operator certifies"
    );
  }

  /// <summary>
  /// bench 618-1090. Kiki copies Restoration Angel; the copy's ETB "you MAY exile target … then return"
  /// blinks Kiki (exile+return), so Kiki re-enters untapped. Two reasons this is AMBER, not GREEN:
  /// (1) the inherited ability is <c>optional</c> ("you may") — the operator can't certify it always
  /// fires; (2) the closing hop is a BLINK (exile→return-to-battlefield→re-enter-untapped), which is a
  /// SEPARATE flow arm out of this feature's scope. Copy-inheritance grafts the ports (making the combo
  /// recognizable); the loop only floors to AMBER until the blink arm lands.
  ///
  /// <para>Target: <b>AMBER</b>. (Restoration Angel is a non-legendary creature → the copy filter
  /// Subsumes it → graftable; the AMBER is soundly irreducible here, not a fudge.)</para>
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Kiki_x_restoration_angel_reconstructs_amber_pending_the_blink_arm()
  {
    var graphs = new[]
    {
      Walk("", "KikiJikiMirrorBreaker.json", "Kiki-Jiki, Mirror Breaker"),
      Walk("", "RestorationAngel.json", "Restoration Angel"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Kiki", StringComparison.Ordinal))
    );

    Assert.That(loop, Is.Not.Null, "the copy of Restoration Angel should graft and be recognized");
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "optional inherited ability + blink hop (separate arm) → soundly AMBER, not GREEN"
    );
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD (the key soundness risk, Decision 3). "Copy of target creature" can copy ANY
  /// legal creature, so a naive graft would manufacture an edge between Kiki and every creature. Kiki +
  /// a vanilla creature (Grizzly Bears — a 2/2 with no abilities) must produce <b>NO cycle</b>: the graft
  /// is admissible (a bear is a non-legendary creature you control) but the grafted copy carries no
  /// ability that acts back on Kiki, so no closing edge exists and the cycle finder reports nothing. The
  /// grafted-but-dead ports are harmless.
  ///
  /// <para>This pins the guard's SECOND layer (closure by the existing arms): admissibility alone never
  /// reports a combo.</para>
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Kiki_x_vanilla_creature_manufactures_no_cycle_the_false_positive_guard()
  {
    // A minimal vanilla creature graph: a single inert ability-less body — nothing that loops back.
    var bear = new PortGraph();
    var graphs = new[] { Walk("", "KikiJikiMirrorBreaker.json", "Kiki-Jiki, Mirror Breaker"), bear };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    Assert.That(
      cycles.Any(c => c.Edges.Any(e => e.From.Card.Contains("Kiki", StringComparison.Ordinal))),
      Is.False,
      "copying a vanilla creature grafts no looping ability — no combo may be manufactured"
    );
  }
}
