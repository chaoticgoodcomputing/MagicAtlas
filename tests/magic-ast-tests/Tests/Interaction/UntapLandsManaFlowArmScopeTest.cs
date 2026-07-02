namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Untap-lands → mana flow arm — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/adding-a-flow-arm.md</c>). Peregrine Drake's ETB "untap up to five
/// lands" makes those lands a free mana source: they tap for mana again (CR 305.4 / 605), and that mana
/// refunds a <c>pay:mana</c> cost — the SHARED enabler the blink-etb-refuel and displacer-cast-blink
/// families turn on. The arm connects an <c>emit:untap</c> whose target is land(s) to a <c>pay:mana</c>
/// consume. It is the third leg of the mana-untap blink loop, alongside:
/// <list type="bullet">
/// <item><b>spell-cast cost → blink</b> (parse-layer): a SPELL ability's mana cost (Ghostly Flicker's
/// {2}{U}) is a <c>pay:mana</c> consume driving its <c>emit:blink</c> (CR 601.2f) — the inbound hop the
/// untapped lands' mana feeds.</item>
/// <item><b>blink → etb</b> (existing arm): the flicker spell blinks Peregrine Drake → Drake's ETB
/// re-fires → untaps lands.</item>
/// <item><b>untap(land) → pay:mana</b> (THIS arm): the untapped lands' mana recasts the spell.</item>
/// </list>
///
/// <para><b>Tier: AMBER, never GREEN</b> — soundly irreducible on TWO axes, neither fudged
/// (adding-a-flow-arm anti-pattern 2/3): (1) the untapped lands' COLOURS and COUNT are unknown ("up to
/// five lands" of unstated colour), so the engine can never certify they cover the {U} pip (CR 107.4) —
/// the untap→pay edge is tiered AMBER explicitly, never the scalar null-default GREEN; (2) the blink
/// targets "artifacts, creatures, and/or lands you control", which only INTERSECTS (≠ Subsumes) Drake's
/// "this creature" ETB, so the operator can't certify the flicker picks Drake. Both floor the loop to a
/// sound AMBER — the loop is structurally feasible (a real net-positive infinite in play) but uncertain
/// from card text alone. Earning GREEN would need a parse-layer sharpen, never an engine relaxation.</para>
/// </summary>
[TestFixture]
public class UntapLandsManaFlowArmScopeTest
{
  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortGraph Walk(string file, string card)
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "HandParsedCards",
      file
    );
    var gold = JsonNode.Parse(File.ReadAllText(path));
    var manaCost = (gold!["Output"]?["Attributes"] as JsonArray)
      ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
      ?["Symbols"];
    return new PortWalk(Ontology).Project(card, gold!["Output"]!["Oracle"]!["Abilities"], manaCost);
  }

  /// <summary>
  /// LEAD MANA-UNTAP BLINK COMBO — Ghostly Flicker + Peregrine Drake (a mana-untap blink in the bench's
  /// 4 Drake-enabled missed combos). Ghostly Flicker ({2}{U}) "exile two target artifacts, creatures,
  /// and/or lands you control, then return those cards" blinks Peregrine Drake; Drake's ETB "untap up to
  /// five lands" refunds the mana to recast Ghostly Flicker — a net-positive infinite. The loop closes:
  /// blink → Drake's ETB (blink arm) → untap lands (Drake's card-defined edge) → Ghostly Flicker's
  /// pay:mana (THIS arm) → blink (the spell-cast cost's card-defined edge).
  ///
  /// <para>Target: <b>AMBER</b> — the untapped lands' colours/count are uncertain (can't certify {U},
  /// CR 107.4) AND the blink only intersects Drake's self-ETB. Sound AMBER, not GREEN, not a fudge.</para>
  /// </summary>
  [Test]
  public void Ghostly_flicker_x_peregrine_drake_reconstructs_amber_untap_lands_refuel()
  {
    var graphs = new[]
    {
      Walk("GhostlyFlicker.json", "Ghostly Flicker"),
      Walk("PeregrineDrake.json", "Peregrine Drake"),
    };
    var engine = new PortGraphEngine(Ontology);
    // Mirror the product/bench reconstruction reach — NOT the unbounded default — so this scope test can
    // only assert a flip the product would actually reconstruct (adding-a-flow-arm.md anti-pattern 5).
    var cycles = engine.FindCycles(engine.Materialize(graphs), PortGraphEngine.DefaultReconstructionReach);

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Ghostly", StringComparison.Ordinal))
      && c.Edges.Any(e => e.From.Card.Contains("Peregrine", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:untap", StringComparison.Ordinal)
        && e.To.Label.StartsWith("pay:mana", StringComparison.Ordinal)
      )
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "Drake's untapped lands should refuel Ghostly Flicker's recast (the untap-lands → mana arm), "
        + "closing the mana-untap blink loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "untapped lands' colours/count are unknown (can't certify {U}, CR 107.4) and the blink only "
        + "intersects Drake's self-ETB → the loop floors to a sound AMBER, never GREEN"
    );

    // The arm hop itself is the explicit AMBER (Unknown reliability) — never the scalar null-default GREEN.
    var armHop = loop.Edges.Single(e =>
      e.From.Label.StartsWith("emit:untap", StringComparison.Ordinal)
      && e.To.Label.StartsWith("pay:mana", StringComparison.Ordinal)
    );
    Assert.That(armHop.Tier, Is.EqualTo(CertaintyTier.Amber));
    Assert.That(armHop.Reliability, Is.EqualTo(Trilean.Unknown), "explicit AMBER, not a scalar null-default GREEN");
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD. The arm fires ONLY when the untap's target is land(s) — untapped lands are
  /// the mana source. A creature/artifact untap (Corridor Monitor's "untap target artifact or creature")
  /// is NOT a mana source; it must yield NO untap → pay:mana edge, so it can never manufacture mana to
  /// feed a cost. Here a synthesized "untap target creature" emit beside a pay:mana cost must produce no
  /// such edge.
  /// </summary>
  [Test]
  public void Non_land_untap_feeds_no_mana_the_false_positive_guard()
  {
    var creatureUntap = new PortNode
    {
      Card = "Untapper",
      Label = "emit:untap",
      Side = PortSide.Emit,
      Identity = "Untapper::emit:untap",
      Subject = new ObjectFilter { CardTypes = ["creature"] },
    };
    var pay = new PortNode
    {
      Card = "Activated",
      Label = "pay:mana",
      Side = PortSide.Consume,
      Quantity = 1,
      Identity = "Activated::pay:mana",
    };

    var engine = new PortGraphEngine(Ontology);
    var edges = engine.Materialize(
      [new PortGraph { Ports = [creatureUntap, pay] }]
    );

    Assert.That(
      edges.Any(e =>
        e.From.Label.StartsWith("emit:untap", StringComparison.Ordinal)
        && e.To.Label.StartsWith("pay:mana", StringComparison.Ordinal)
      ),
      Is.False,
      "untapping a creature is not a mana source — no untap → pay:mana edge may be manufactured"
    );
  }
}
