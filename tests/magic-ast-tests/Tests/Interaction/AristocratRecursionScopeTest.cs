namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Aristocrat recursion (cast-from-graveyard) — ACCEPTANCE PINS (see
/// <c>libs/mast-interaction/docs/aristocrat-recursion-scope.md</c>). All three pins are
/// <c>[Ignore]</c>d: the graveyard-recursion arm is DESIGN-ONLY at this point (no engine/parser code),
/// so they MUST skip — not fail — and the suite stays green. They define "aristocrat-recursion done."
///
/// <para>The canonical loop: a free/cheap <b>sac outlet</b> sacrifices Gravecrawler → GC <b>dies</b> to
/// the graveyard (death payoff fires) → GC is <b>recast from the graveyard</b> ({1}{B}, gated on "you
/// control a Zombie" — GC is itself a Zombie) → GC <b>re-enters</b> → refuels the sac. The engine has no
/// cast-from-graveyard arm today: <c>alternativeCast</c>/<c>FromZone:Graveyard</c> is coarse
/// (<c>known-coarse-projections.json</c>, "no flow rule consumes it yet"), so the loop never closes.</para>
///
/// <para>The three pins pin the SCOPE's central tiering call (scope §4):</para>
/// <list type="bullet">
/// <item>Gravecrawler + Pitiless Plunderer + Ashnod's Altar → <b>GREEN</b>. Two mana sources cover the
/// {1}{B} recast: Ashnod's {C}{C} pays the generic {1}; Pitiless's Treasure (<c>emit:mana:any</c>) pays
/// the black {B} pip (CR 107.4 — colourless can't pay a coloured pip, but the Treasure's any-pool can).
/// Mana-positive ⇒ §8 <c>ManaBalanced</c> + <c>ManaProductive</c> certify ⇒ GREEN.</item>
/// <item>Warren Soultrader + Gravecrawler + Blood Artist → <b>AMBER</b>. The honest tier: Warren makes
/// ONE Treasure per sac = 1 <c>any</c> mana, but the recast is {1}{B} = 2 mana, so the generic {1} is
/// provably short at 1 mana/iter ⇒ §8 <c>ManaBalanced</c> floors it to AMBER even though the drain is a
/// productive output. Earning GREEN later needs a §8 steady-state (Treasure-accumulation) argument or a
/// second mana source — never an engine fudge (adding-a-flow-arm anti-pattern 2).</item>
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
  /// sacs GC for free → GC dies → Pitiless makes a Treasure → GC recast {1}{B} → re-enter → re-sac. The
  /// recast's two-mana cost is covered by TWO sources: Ashnod's {C}{C} pays the generic {1}, the
  /// Treasure's any-colour mana pays the black {B} pip. Mana-positive (a Treasure surplus accrues each
  /// death) ⇒ §8 certifies infinite.
  ///
  /// <para>Target: <b>GREEN</b>. The per-colour balance (PortGraphEngine.ManaBalanced) genuinely covers
  /// {1}{B}; the loop nets an unbounded resource (Treasures/mana) ⇒ ManaProductive holds. The §8-B
  /// one-shot-self-removal carve-out must RETAIN the self-death cycle because the recast is a
  /// self-<c>emit:returntobattlefield</c> (the dual of Persist/Undying). interaction-judge must PROCEED
  /// on this GREEN — recast loops are a prime false-positive surface.</para>
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
  /// (Pay 1 life, Sacrifice another creature → ONE Treasure) → GC dies → Blood Artist drains → GC recast
  /// {1}{B} → re-enter → re-sac. The honest tier is AMBER, not GREEN: Warren makes a single Treasure per
  /// iteration = 1 any-colour mana, but the recast is {1}{B} = 2 mana, so the generic {1} is provably
  /// unfed at 1 mana/iter. §8 ManaBalanced floors the provable shortfall regardless of the drain output.
  ///
  /// <para>Target: <b>AMBER</b>. The drain is a productive output, but a provable mana shortfall floors
  /// the loop (PortGraphEngine.ManaBalanced — the same machinery that floors Chatterfang × Ruthless
  /// Knave). This is the soundness-preserving call: the GREEN would have to be EARNED by a §8
  /// steady-state argument (Treasures accumulate across iterations) or a parse sharpen, never fudged
  /// (adding-a-flow-arm anti-pattern 2). The Warren+Zulaport sibling (pop 34 860) is the same shape.</para>
  /// </summary>
  [Test]
  public void Warren_x_gravecrawler_x_blood_artist_reconstructs_amber_mana_short_recast()
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
      "1 Treasure/iter vs a {1}{B}=2 recast → provable {1} shortfall → §8 floors to AMBER (not a fudge to GREEN)"
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
