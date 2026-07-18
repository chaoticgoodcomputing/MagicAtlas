namespace MagicAST.Interaction.Tests;

using System.Text.Json;
using MagicAST.AST.References;
using MagicAST.Interaction;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Initiative 06 — Track A (operator axes: <c>IsSelf</c> + <c>Resource.Subject</c>) — <b>scope/target</b>
/// tests. Each is <c>[Ignore]</c>d so it documents the desired tier WITHOUT running (the suite stays
/// green); together they define "operator axes complete." See
/// <c>libs/mast-interaction/docs/06-operator-axes-plan.md</c>.
///
/// <para>The measurement: the <c>IsSelf</c> axis is already landed end-to-end (parser → operator →
/// label → engine, rounds 2/3/4) — the exemplars below pin that it stays <b>correctly pruned / Amber</b>
/// (a regression definition of "still done", per ADR-0002 §8-B / §10). The genuinely-open axis is
/// <c>Resource.Subject</c> — the positional-counter <c>@bearer</c> facet (ADR-0002 §3) — which has NOT
/// landed; the counter-flow exemplar pins the desired GREEN once it does. No exemplar here is blocked by
/// the IsSelf axis; the bench's (as of 2026-06-16) measured gap was the Squirrel⊄creature type straddle
/// (8 Amber) and absent flow arms (25 Missed), neither in this track's scope. 2026-07-18: the straddle's
/// <b>sacrifice-cost</b> instance (Chatterfang × Pitiless Plunderer, CR 701.21a guarantees sac fodder is
/// already a permanent) is fixed — <c>PortGraph</c>'s sacrifice-cost Subject now lifts a subtype-only
/// filter to its permanent card type, see <c>PortGraphEngineTest.Reconstructs_the_chatterfang_pitiless_free_loop_as_green</c>
/// — but the general <c>ObjectFilterRelations.Subsumes</c> contract (a context-free subtype filter can't
/// assume a permanent) is unchanged, so the straddle may still gate other non-sac port roles; the exact
/// remaining count needs a fresh bench run, not re-derived here.</para>
/// </summary>
[TestFixture]
public class OperatorAxesScopeTest
{
  private const string Pending = "06 — pending operator/Resource.Subject completion";

  private static readonly TypeOntology Ontology = JsonSerializer.Deserialize<TypeOntology>(
    File.ReadAllText(TestData.OntologyPath)
  )!;

  private static PortNode Consume(string card, string label, ObjectFilter? subj, int? qty = 1) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Consume,
      Subject = subj,
      Quantity = qty,
      Identity = card + "::" + label,
    };

  private static PortNode Emit(string card, string label, ObjectFilter? subj, int? qty = 1) =>
    new()
    {
      Card = card,
      Label = label,
      Side = PortSide.Emit,
      Subject = subj,
      Quantity = qty,
      Identity = card + "::" + label,
    };

  private static bool ClosesAcross(IReadOnlyList<PortCycle> cycles, string card) =>
    cycles.Any(c => c.Edges.Any(e => e.From.Card == card || e.To.Card == card));

  // ── IsSelf axis — regression "still done" pins (these are EXPECTED to already pass once un-Ignored;
  //    they exist to make "IsSelf complete" a checkable contract, not new behaviour). ───────────────

  /// <summary>
  /// Round-2 D#61/D#62/D#111 exemplars (Chromatic Star / Dromar's Attendant / Barrels of Blasting Jelly):
  /// a "Sacrifice this" self-sac producer fires <b>once</b> — a created token can never refuel its
  /// <c>:self</c> sac (CR 400.7). So a loop that tries to refuel the self-sac from its own tokens must NOT
  /// close (pruned), and a same-shape <em>non-self</em> sac (a type-based "sacrifice a Treasure") MUST
  /// close — proving it is the IsSelf-ness that prunes. (Landed `4d3235b6`; pinned as the contract.)
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Self_sac_producer_is_one_shot_a_token_cannot_refuel_its_own_self_sac()
  {
    var engine = new PortGraphEngine(Ontology);

    // "Sacrifice this artifact: …" self-sac fed only by the artifact tokens the loop itself creates.
    var selfSac = Consume(
      "Barrels",
      "sac:artifact:self",
      new ObjectFilter { CardTypes = ["artifact"], IsSelf = true }
    );
    var maker = Emit(
      "Maker",
      "emit:token:artifact:treasure:controlled",
      new ObjectFilter
      {
        CardTypes = ["artifact"],
        Subtypes = ["Treasure"],
        IsToken = true,
        Controller = ControllerFilter.You,
      }
    );
    var selfGraph = new PortGraph
    {
      Ports = [selfSac, maker],
      CardDefinedEdges = [new() { From = selfSac, To = maker }],
    };
    Assert.That(
      ClosesAcross(engine.FindCycles(engine.Materialize([selfGraph]), maxLength: 5), "Barrels"),
      Is.False,
      "a created token can never refuel a :self sac (CR 400.7) — the one-shot self-sac must be pruned"
    );

    // Control: a non-self type-sac ("sacrifice a Treasure", IsSelf null) IS refuellable → closes.
    var typeSac = Consume(
      "Outlet",
      "sac:artifact:treasure:controlled",
      new ObjectFilter
      {
        CardTypes = ["artifact"],
        Subtypes = ["Treasure"],
        Controller = ControllerFilter.You,
      }
    );
    var typeGraph = new PortGraph
    {
      Ports = [typeSac, maker],
      CardDefinedEdges = [new() { From = typeSac, To = maker }],
    };
    Assert.That(
      ClosesAcross(engine.FindCycles(engine.Materialize([typeGraph]), maxLength: 5), "Outlet"),
      Is.True,
      "a non-self type-sac is refuellable by created tokens — proving the prune keys on IsSelf, not shape"
    );
  }

  /// <summary>
  /// Self-death exemplars (Doomed Dissenter / Brindle Shoat / Elenda, ADR-0002 §10): "when this creature
  /// dies, create a token" is structurally non-repeatable (the source dies once, CR 400.7), so a loop fed
  /// by a free sac outlet through the <c>ltb:…:to-graveyard:self</c> trigger must be PRUNED, while the
  /// same shape with a non-self ("another creature dies") trigger is a real repeatable loop — retained.
  /// (Landed via the §8-B prune + the self-binding; pinned as the contract.)
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Self_death_token_loop_is_pruned_while_another_creature_death_is_retained()
  {
    var engine = new PortGraphEngine(Ontology);
    var outlet = Consume(
      "Outlet",
      "sac:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You }
    );

    var selfDies = Consume(
      "Elenda",
      "ltb:creature:to-graveyard:self",
      new ObjectFilter { CardTypes = ["creature"], IsSelf = true }
    );
    var selfToken = Emit(
      "Elenda",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var selfGraph = new PortGraph
    {
      Ports = [selfDies, selfToken, outlet],
      CardDefinedEdges = [new() { From = selfDies, To = selfToken }],
    };
    Assert.That(
      ClosesAcross(engine.FindCycles(engine.Materialize([selfGraph]), maxLength: 5), "Outlet"),
      Is.False,
      "self-death → token + sac outlet is one-shot (source dies once) — pruned, not surfaced"
    );

    var otherDies = Consume(
      "Pitiless",
      "ltb:creature:to-graveyard:controlled:another",
      new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
        ExcludeSelf = true,
      }
    );
    var otherToken = Emit(
      "Pitiless",
      "emit:token:creature:controlled",
      new ObjectFilter { CardTypes = ["creature"], IsToken = true, Controller = ControllerFilter.You }
    );
    var otherGraph = new PortGraph
    {
      Ports = [otherDies, otherToken, outlet],
      CardDefinedEdges = [new() { From = otherDies, To = otherToken }],
    };
    Assert.That(
      ClosesAcross(engine.FindCycles(engine.Materialize([otherGraph]), maxLength: 5), "Outlet"),
      Is.True,
      "another-creature death is a real repeatable loop — proving it is the IsSelf-ness that prunes"
    );
  }

  /// <summary>
  /// IsSelf follow-on (1), ADR-0002 §10 <i>Residual</i> — keyword self-triggers on a <b>non-death</b>
  /// event (Melee → "whenever THIS creature attacks"; Evoke → enters) still bypass self-binding today.
  /// When the attacks/enters flow arms exist, a loop closing through a source's OWN attack trigger must be
  /// scoped <c>:self</c> so it floors/prunes like the self-death case rather than false-bridging from
  /// another card's attack. This is a genuine remaining IsSelf gap (no current false-GREEN only because
  /// the attacks flow arm is not yet modeled).
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Keyword_self_trigger_on_a_non_death_event_self_binds()
  {
    // Definition of done: a "whenever this creature attacks" trigger projects an attacks-consume scoped
    // :self, so the operator demotes a cross-card attack→this bridge to Amber (the dual of the
    // self-death binding). Today the parser drops the self-ness on non-death keyword events, so the
    // projected subject carries IsSelf=false/null — this pins the desired IsSelf=true.
    var selfAttacker = new ObjectFilter { CardTypes = ["creature"], IsSelf = true };
    Assert.That(
      selfAttacker.IsSelf,
      Is.True,
      "a Melee/Evoke keyword self-trigger must self-bind its subject (ADR-0002 §10 residual)"
    );
  }

  // ── Resource.Subject axis — the GENUINELY-OPEN work: the positional-counter @bearer facet. ───────

  /// <summary>
  /// The one Resource.Subject exemplar that defines "axis complete" (ADR-0002 §3 — the
  /// <c>counter:…@&lt;bearer&gt;</c> parameter facet that fixes <c>Resource.Subject = null</c>). A
  /// counter-flow loop — a per-iteration <c>emit:counter:plus-one-plus-one</c> whose <b>bearer</b> the
  /// loop re-feeds, consumed by a counter-gated outlet on the <em>same bearer</em> — must derive a
  /// counter-flow edge tiered by <see cref="ObjectFilterRelations.Intersects"/> on the bearer
  /// <see cref="ObjectFilter"/>, and certify GREEN when the bearers match. Today there is NO bearer facet
  /// and NO counter-flow edge, so the loop does not even close; this pins the target. (No such loop is in
  /// the 33-combo bench — the blocked cohort for this axis is ~0 in the surfaced corpus, so this is a
  /// precision/correctness target, not a recall unblock.)
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Counter_flow_certifies_only_when_the_bearer_matches()
  {
    var engine = new PortGraphEngine(Ontology);

    // A bearer the loop sustains (a creature it controls).
    var bearer = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You };

    // emit:counter:plus-one-plus-one @bearer  (TARGET label shape — the @bearer parameter facet).
    var counterEmit = Emit("Doubler", "emit:counter:plus-one-plus-one", bearer);
    // A counter-gated outlet that CONSUMES a +1/+1 counter off the SAME bearer each iteration.
    var counterSink = Consume("Sink", "consume:counter:plus-one-plus-one", bearer);
    // The card-defined return that re-feeds the emit (closes the loop).
    var refeed = Emit("Sink", "emit:counter:plus-one-plus-one", bearer);

    var graph = new PortGraph
    {
      Ports = [counterEmit, counterSink, refeed],
      CardDefinedEdges = [new() { From = counterSink, To = refeed }],
    };
    var cycles = engine.FindCycles(engine.Materialize([graph]), maxLength: 5);
    Assert.That(cycles, Is.Not.Empty, "a matched-bearer counter loop must close once @bearer is derived");
    Assert.That(
      cycles.Max(c => c.Tier),
      Is.EqualTo(CertaintyTier.Green),
      "a matched-bearer counter-flow loop certifies GREEN (Intersects on the bearer = Overlaps, Yes)"
    );
  }

  /// <summary>
  /// The negative half of the Resource.Subject axis (ADR-0002 §3 — the collision the bearer facet fixes:
  /// "a counter on Ballista ≡ a counter anywhere"). A counter emitted onto an <b>opponent's</b> permanent
  /// must NOT feed a "you-control" counter-gated outlet — the bearers are <see cref="FilterRelation"/>
  /// disjoint on the control axis, so the edge must be pruned (no false-GREEN). Today, with no bearer
  /// facet, the coarse <c>emit:counter:…:target</c> projection cannot distinguish bearers, so this
  /// collision is open; this pins the desired prune.
  /// </summary>
  [Test]
  [Ignore(Pending)]
  public void Counter_flow_is_pruned_when_the_bearers_are_disjoint()
  {
    var engine = new PortGraphEngine(Ontology);

    var myBearer = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You };
    var oppBearer = new ObjectFilter
    {
      CardTypes = ["creature"],
      Controller = ControllerFilter.Opponent,
    };

    var counterOnOpp = Emit("OppDoubler", "emit:counter:plus-one-plus-one", oppBearer);
    var myCounterSink = Consume("Sink", "consume:counter:plus-one-plus-one", myBearer);
    var refeed = Emit("Sink", "emit:counter:plus-one-plus-one", myBearer);

    var graph = new PortGraph
    {
      Ports = [counterOnOpp, myCounterSink, refeed],
      CardDefinedEdges = [new() { From = myCounterSink, To = refeed }],
    };
    Assert.That(
      ClosesAcross(engine.FindCycles(engine.Materialize([graph]), maxLength: 5), "OppDoubler"),
      Is.False,
      "a counter on an opponent's permanent cannot feed a you-control counter outlet — disjoint bearers prune"
    );
  }
}
