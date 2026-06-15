namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Aristocrat recursion (cast-from-graveyard) — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/aristocrat-recursion-scope.md</c>). The graveyard-recursion arm is now
/// BUILT and interaction-judge-verified; all three pins are LIVE (no <c>[Ignore]</c>) and pass.
///
/// <para>The canonical loop: a free/cheap <b>sac outlet</b> sacrifices Gravecrawler → GC <b>dies</b> to
/// the graveyard (death payoff fires) → GC is <b>recast from the graveyard</b> (for its own mana cost
/// <b>{B}</b> — its <c>alternativeCast</c> states no cost, CR 601.3e — gated on "you control a Zombie";
/// GC is itself a Zombie) → GC <b>re-enters</b> → refuels the sac.</para>
///
/// <para>The three pins pin the SCOPE's central tiering call (scope §4), as corrected by the judge
/// (<c>docs/judgments/verdict-2026-06-15-aristocrat-recursion.json</c>):</para>
/// <list type="bullet">
/// <item>Gravecrawler + Pitiless Plunderer + Ashnod's Altar → <b>GREEN</b>. A genuine infinite loop: the
/// single <b>{B}</b> recast is paid by Pitiless's Treasure (<c>emit:mana:any</c>, CR 107.4), with
/// Ashnod's {C}{C} pure surplus. Mana-positive ⇒ §8 <c>ManaBalanced</c> + <c>ManaProductive</c> ⇒ GREEN.
/// (A 7-hop cycle: the engine's unbounded <c>FindCycles</c> here tiers it GREEN; the product/bench
/// <c>LengthBound=5</c> can't reach it and reads it AMBER — a separate length-bound decision.)</item>
/// <item>Warren Soultrader + Gravecrawler + Blood Artist → <b>AMBER</b>. NOT a mana shortfall (the {B}
/// recast is fully paid by Warren's one Treasure). The honest floor is Warren's <b>unfed <c>Pay 1 life</c>
/// co-cost</b>: §8 <c>ConjunctionHolds</c> needs it loop-fed, but there is no <c>(life, pay)</c> flow arm
/// (only <c>(life, trigger)</c>), so <c>CoCostsSatisfied=false</c> → AMBER (CR 118.3/119.4 — life is a
/// real resource MAST can't certify the loop refills). Sound AMBER, not a false negative.</item>
/// <item>Gravecrawler with its cast-from-graveyard permission ABSENT + a free sac outlet → <b>NO
/// cycle</b>. The false-positive guard (scope §4, layer i): the recursion arm fires ONLY on a real
/// cast-from-graveyard permission. A dead creature with no recast permission stays dead — a naive
/// "anything in a graveyard refuels a sac" arm would manufacture combos in every aristocrat deck.</item>
/// </list>
/// </summary>
[TestFixture]
public class AristocratRecursionScopeTest
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
  /// recast's pay:mana co-cost source for the cast-from-graveyard arm (Gravecrawler is cast for its own
  /// mana cost, CR 601.3e). Null when the card has no manaCost attribute.</summary>
  private static JsonNode? ManaCostSymbols(JsonNode? gold) =>
    (gold?["Output"]?["Attributes"] as JsonArray)
      ?.FirstOrDefault(a => a?["Kind"]?.ToString() == "manaCost")
      ?["Symbols"];

  /// <summary>
  /// LEAD GREEN COMBO (bench: Gravecrawler + Pitiless Plunderer + Ashnod's Altar, pop 32 582). Ashnod's
  /// sacs GC for free → GC dies → Pitiless makes a Treasure → GC recast for <b>{B}</b> → re-enter →
  /// re-sac. The single black pip is paid by the Treasure's any-colour mana (CR 107.4); Ashnod's {C}{C}
  /// is pure surplus. Mana-positive (a Treasure surplus accrues each death) ⇒ §8 certifies infinite.
  ///
  /// <para>Target: <b>GREEN</b>. The per-colour balance (PortGraphEngine.ManaBalanced) genuinely covers
  /// the {B} recast; the loop nets an unbounded resource (Treasures/mana) ⇒ ManaProductive holds. The
  /// §8-B one-shot-self-removal carve-out keys on the self-<c>emit:returntobattlefield</c> label (the
  /// dual of Persist/Undying) — here it's inert (no self-LTB on the cycle) but wired correctly.
  /// interaction-judge PROCEEDed on this GREEN — recast loops are a prime false-positive surface. Run via
  /// unbounded <c>FindCycles</c> (the 7-hop loop exceeds the product LengthBound).</para>
  /// </summary>
  [Test]
  public void Gravecrawler_x_pitiless_x_ashnods_altar_reconstructs_green_mana_positive_recast()
  {
    var graphs = new[]
    {
      Walk("", "Gravecrawler.json", "Gravecrawler"),
      Walk("RIX", "PitilessPlunderer.json", "Pitiless Plunderer"),
      Walk("ATQ", "AshnodsAltar.json", "Ashnod's Altar"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    // The recursion loop traverses Gravecrawler's recast (a self return-to-battlefield) refueling a sac.
    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Gravecrawler", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
      )
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "Gravecrawler's recast should refuel Ashnod's sac and close the recursion loop"
    );
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Green),
      "two mana sources cover the {1}{B} recast (Ashnod's {C}{C} → {1}, Treasure any → {B}) — §8 certifies"
    );
  }

  /// <summary>
  /// AMBER COMBO (bench: Warren Soultrader + Gravecrawler + Blood Artist, pop 38 733). Warren sacs GC
  /// (<b>Pay 1 life</b>, Sacrifice another creature → ONE Treasure) → GC dies → Blood Artist drains → GC
  /// recast for {B} → re-enter → re-sac. The honest tier is AMBER, not GREEN — but NOT for a mana reason:
  /// the {B} recast is fully paid by Warren's one Treasure (no shortfall). The floor is Warren's
  /// <b>unfed <c>Pay 1 life</c> co-cost</b>: §8 <c>ConjunctionHolds</c> requires every co-cost sibling
  /// loop-fed, and there is no <c>(life, pay)</c> flow arm (Blood Artist's <c>emit:life:gain</c> feeds a
  /// life-TRIGGER, never a life-COST), so <c>CoCostsSatisfied=false</c>.
  ///
  /// <para>Target: <b>AMBER</b>. The drain is a productive output, but the loop can't certify it refills
  /// the paid life each iteration (CR 118.3/119.4 — life is a real resource; MAST has no life-as-resource
  /// ledger). Sound AMBER, not Red (the loop is structurally feasible), not a false negative. Earning
  /// GREEN later needs a real <c>(life, pay)</c> arm or a life-ledger, never an engine fudge
  /// (adding-a-flow-arm anti-pattern 2). The Warren+Zulaport sibling (pop 34 860) is the same shape.</para>
  /// </summary>
  [Test]
  public void Warren_x_gravecrawler_x_blood_artist_reconstructs_amber_unfed_life_cocost()
  {
    var graphs = new[]
    {
      Walk("LCI", "WarrenSoultrader.json", "Warren Soultrader"),
      Walk("", "Gravecrawler.json", "Gravecrawler"),
      Walk("", "BloodArtist.json", "Blood Artist"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var loop = cycles.FirstOrDefault(c =>
      c.Edges.Any(e => e.From.Card.Contains("Gravecrawler", StringComparison.Ordinal))
      && c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
      )
    );

    Assert.That(loop, Is.Not.Null, "the recursion loop should be recognized (recast refuels Warren's sac)");
    Assert.That(
      loop!.Tier,
      Is.EqualTo(CertaintyTier.Amber),
      "Warren's unfed 'Pay 1 life' co-cost (no (life,pay) arm) → ConjunctionHolds false → §8 floors to AMBER "
        + "(the {B} recast itself is fully paid by the Treasure — not a mana shortfall, not a fudge to GREEN)"
    );
  }

  /// <summary>
  /// FALSE-POSITIVE GUARD (scope §4, guard layer i — the KEY soundness risk). A naive "anything in a
  /// graveyard refuels a sac" arm would manufacture combos in every aristocrat deck: any creature dies,
  /// any free sac outlet, infinite. The recursion arm must fire ONLY on a REAL cast-from-graveyard
  /// permission (Gravecrawler's <c>alternativeCast</c>/<c>FromZone:Graveyard</c>). Here we strip that
  /// permission: a Gravecrawler whose ONLY ability is the inert "can't block" static (no recast) plus a
  /// free sac outlet (Ashnod's Altar) must produce <b>NO cycle</b> — the dead creature stays dead.
  ///
  /// <para>This pins the guard's FIRST layer (admissibility): no recast permission ⇒ no recast emit ⇒ no
  /// (returntobattlefield, sac) edge ⇒ no recursion cycle, exactly as a vanilla creature must not combo.</para>
  /// </summary>
  [Test]
  public void Gravecrawler_without_recast_permission_manufactures_no_cycle_the_false_positive_guard()
  {
    // Gravecrawler with the cast-from-graveyard static REMOVED: only the inert "can't block" ability
    // remains, so it has no recast permission — a plain creature body that dies and stays dead.
    var noRecastAbilities = new JsonArray(
      new JsonObject
      {
        ["Kind"] = "static",
        ["Effects"] = new JsonArray(new JsonObject { ["EffectType"] = "cantBlock" }),
      }
    );
    var gravecrawlerNoRecast = new PortWalk(Ontology).Project("Gravecrawler", noRecastAbilities);

    var graphs = new[]
    {
      gravecrawlerNoRecast,
      Walk("ATQ", "AshnodsAltar.json", "Ashnod's Altar"),
    };
    var engine = new PortGraphEngine(Ontology);
    var cycles = engine.FindCycles(engine.Materialize(graphs));

    var recursionCycle = cycles.Any(c =>
      c.Edges.Any(e =>
        e.From.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
        || e.To.Label.StartsWith("emit:returntobattlefield", StringComparison.Ordinal)
      )
    );

    Assert.That(
      recursionCycle,
      Is.False,
      "no cast-from-graveyard permission ⇒ no recast emit ⇒ no recursion cycle may be manufactured"
    );
  }
}
