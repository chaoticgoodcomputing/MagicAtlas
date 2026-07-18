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
/// <item>Warren Soultrader + Gravecrawler + Blood Artist → <b>GREEN</b> (2026-07-18, was AMBER). NOT a
/// mana shortfall (the {B} recast is fully paid by Warren's one Treasure). The prior floor was Warren's
/// <b>unfed <c>Pay 1 life</c> co-cost</b>: §8 <c>ConjunctionHolds</c> needs it loop-fed, but there was no
/// <c>(life, pay)</c> flow arm (only <c>(life, trigger)</c>), so <c>CoCostsSatisfied=false</c> → AMBER (CR
/// 118.3/119.4 — life is a real resource). A new <c>LifeCostToPay</c> flow arm (PortFlowMatcher +
/// PortGraphEngine.LifeGainFeedsCost, mirroring ManaToPay) now bridges Blood Artist's life-gain emit to
/// Warren's <c>pay:paylife</c> cost, and a new <c>LifeBalanced</c> check (mirroring ManaBalanced, its own
/// <c>PortCycle</c> field so the tier floor stays distinguishable from a mana shortfall) requires net life
/// ≥ 0 per iteration (CR 119.4 paying life is a real bounded loss; CR 119.6 ends the game at 0-or-less
/// life, so a net-negative life loop is finite, not infinite) — genuinely satisfied here (1 life gained
/// exactly offsets 1 life paid). No longer a false negative.</item>
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
  /// GREEN COMBO (bench: Warren Soultrader + Gravecrawler + Blood Artist, pop 38 733; 2026-07-18, was
  /// AMBER). Warren sacs GC (<b>Pay 1 life</b>, Sacrifice another creature → ONE Treasure) → GC dies →
  /// Blood Artist drains (and gains Warren's controller 1 life) → GC recast for {B} → re-enter → re-sac.
  /// The {B} recast is fully paid by Warren's one Treasure (no mana shortfall — never the issue here).
  ///
  /// <para>Formerly floored to AMBER by Warren's <b>unfed <c>Pay 1 life</c> co-cost</b>: §8
  /// <c>ConjunctionHolds</c> requires every co-cost sibling loop-fed, and there was no <c>(life, pay)</c>
  /// flow arm (Blood Artist's <c>emit:life:gain</c> fed only a life-TRIGGER, never a life-COST), so
  /// <c>CoCostsSatisfied=false</c>. The 2026-07-18 precision-fix adds <c>PortFlowMatcher.FlowArm.LifeCostToPay</c>
  /// (a new <c>PayLifeFamily</c> stems <c>pay:paylife</c> as its own <c>paylife</c> consume; the guard
  /// <c>PortGraphEngine.LifeGainFeedsCost</c> requires a GAIN-direction emit, mirroring
  /// <c>ManaColorFeeds</c>) plus a new <c>PortCycle.LifeBalanced</c> field (mirroring
  /// <c>Balanced</c>/<c>ManaBalanced</c>) requiring the loop's net life ≥ 0 per iteration — CR-grounded:
  /// CR 119.4 makes paying life a real, bounded loss and CR 119.6 ends the game at 0-or-less life, so a
  /// net-negative life loop would kill its own caster after finitely many iterations and is not
  /// certifiable infinite; "some life gain exists" alone would NOT be sufficient. Here Blood Artist's "you
  /// gain 1 life" exactly offsets Warren's "Pay 1 life" (net 0, non-negative) — genuinely GREEN, not a
  /// fudge (adding-a-flow-arm anti-pattern 2). The Warren+Zulaport sibling (pop 34 860) is the same shape,
  /// also now GREEN.</para>
  /// </summary>
  [Test]
  public void Warren_x_gravecrawler_x_blood_artist_reconstructs_green_life_cost_fed()
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
      && c.Tier == CertaintyTier.Green
    );

    Assert.That(
      loop,
      Is.Not.Null,
      "the recursion loop should be recognized (recast refuels Warren's sac) and certify GREEN now that "
        + "Blood Artist's life-gain feeds Warren's pay:paylife co-cost (LifeCostToPay arm) and the loop is "
        + "life-balanced (net 0)"
    );
    Assert.That(loop!.CoCostsSatisfied, Is.True, "the life co-cost is now loop-fed by Blood Artist's life-gain");
    Assert.That(loop.LifeBalanced, Is.True, "1 life gained (Blood Artist) exactly offsets 1 life paid (Warren) per iteration");
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
