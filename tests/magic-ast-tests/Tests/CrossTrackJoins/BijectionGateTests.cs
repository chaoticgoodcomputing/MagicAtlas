namespace MagicAST.Tests.Tests.CrossTrackJoins;

using MagicAST.Interaction;
using MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

/// <summary>
/// The <b>ADR-0004 §2 / Stage 6 bijection gate — soundness half, the CODE leg</b> (issue #34). This is
/// the one-time closure gate; the standing per-loop check is issue #39.
///
/// <para><b>What it closes.</b> The sibling <see cref="MagicAST.Tests.Tests.CrossTrackJoins.GuardWitnessJoinGateTests"/>
/// (#30) closes the rule↔gold leg from the golds' <c>declares</c>, and REPORTS that the rule↔code leg
/// cannot be checked by a literal rule-id scan (0 of 29 ids appear in <c>libs/**/*.cs</c>). This gate closes
/// that leg <b>structurally</b>: the engine now tags every edge, at formation, with the structural
/// mechanism that formed it (<see cref="PortEdge.Mechanism"/> / <see cref="PortEdge.Arm"/>), and this gate
/// re-runs the <b>real engine</b> over the interaction golds' sentinels and asserts that every rules-defined
/// mechanism it forms is witnessed by a gold — a <c>structure ↔ structure</c> join in which no rule id ever
/// enters the engine or the join.</para>
///
/// <para><b>Live, not stored (§2/§5's vacuity trap).</b> The mechanism inventory is materialized in-process
/// every run (<see cref="EdgeMechanismInventory.Derive"/>), never read from a committed artifact — a gate
/// that reads its own prior output verifies the derivation against itself.</para>
/// </summary>
[TestFixture]
public class BijectionGateTests
{
  private static readonly string Root = CrossTrackSources.RepoRoot();

  private static readonly Lazy<EdgeMechanismInventory.Inventory> LiveInventory = new(EdgeMechanismInventory.Derive);

  private static readonly Lazy<EdgeMechanismBijection.Result> Live = new(() =>
  {
    var inv = LiveInventory.Value;
    var (connectors, goldsWithRules) = EdgeMechanismBijection.LoadGoldConnectors(Root);
    var rollup = EdgeMechanismBijection.LoadRollupRules(Root);
    return EdgeMechanismBijection.Join(
      inv, connectors, rollup, goldsWithRules, EdgeMechanismBijection.ProjectedSentinels(inv));
  });

  // ── non-vacuity (proven first: the whole ADR exists to kill the empty-set pass) ────────────────────

  /// <summary>The live run must have actually run — real sentinels projected, real edges formed, real
  /// rules-mechanisms tagged. A soundness gate that passed on an empty inventory is worthless (§2/§5).</summary>
  [Test]
  public void Live_run_is_non_vacuous()
  {
    var inv = LiveInventory.Value;
    Assert.Multiple(() =>
    {
      Assert.That(inv.SentinelsProjected, Is.GreaterThan(0), "no sentinel projected — the live engine did not run");
      Assert.That(inv.EdgesFormed, Is.GreaterThan(0), "no edge formed");
      Assert.That(inv.Mechanisms, Is.Not.Empty, "no structural mechanism tagged");
      Assert.That(inv.ArmsFired, Is.Not.Empty, "no FlowArm fired — the seam recorded nothing");
      Assert.That(Live.Value.LiveRulesMechanisms, Is.GreaterThan(0), "no RULES-defined mechanism fired — the soundness subject is empty");
      Assert.That(Live.Value.GoldConnectorCount, Is.GreaterThan(0), "no gold declared a rules-connector edge");
      Assert.That(Live.Value.RollupRuleCount, Is.GreaterThan(0), "the rollup carried no rules");
      Assert.That(Live.Value.Vacuous, Is.False);
    });

    TestContext.Out.WriteLine(
      $"live engine run: {inv.SentinelsProjected} sentinels → {inv.EdgesFormed} edges → "
        + $"{inv.Mechanisms.Count} distinct structural mechanisms ({Live.Value.LiveRulesMechanisms} rules-defined); "
        + $"{inv.ArmsFired.Count} FlowArms fired: {string.Join(", ", inv.ArmsFired)}");
  }

  // ── THE GATE (§2.1 soundness): no rules-mechanism the engine can form is unwitnessed ───────────────

  /// <summary>
  /// The <b>acknowledged unwitnessed-capability backlog</b> — an explicit, named whitelist (never a
  /// shrink-only baseline). Each entry is a rules-mechanism the engine can form that <b>no gold declares
  /// and no rollup rule covers</b>: a genuine soundness finding whose fix is authoring a witnessing gold —
  /// judge-gated mast-loop work this ADR gate deliberately does not do (owner ruling 2026-07-22). Listing it
  /// here is the ADR's "register the exception" — it is surfaced, not papered over. The gate asserts the
  /// live unwitnessed set <b>equals</b> this list exactly, so a NEW unwitnessed capability fails RED, and a
  /// RESOLVED one (its gold finally authored) also fails until struck from the list.
  /// </summary>
  private static readonly IReadOnlyDictionary<string, string> AcknowledgedUnwitnessed = new Dictionary<string, string>(StringComparer.Ordinal)
  {
    // FlowArm/LifeCostToPay [life → paylife] — RESOLVED (ADR-0004 #40, 2026-07-22). The life-gain →
    // pay-life-cost arm is now witnessed cross-card by the bloodthirsty-conqueror-x-aetherflux-reservoir
    // interaction gold: Bloodthirsty Conqueror's emit:life:gain feeds Aetherflux Reservoir's consume:paylife (Pay 50 life) —
    // a rules-defined edge declaring policy:life-supplies-cost — so the arm no longer fires only intra-card
    // on Aetherflux. The gate now requires the unwitnessed set to be EMPTY.
  };

  /// <summary>THE GATE (§2.1 soundness): every RulesDefined structural mechanism the live engine forms is
  /// witnessed by a gold — stem-exactly, or (vocabulary-independent) by firing only on golds that declare a
  /// rules-connector edge — <b>except</b> the explicitly acknowledged backlog above. The set of unwitnessed
  /// mechanisms must equal that backlog exactly: a new unwitnessed capability is a RED soundness finding, and
  /// a resolved one forces the ledger to shrink. The fix for any entry is the loop's own move — author the
  /// witnessing gold — never annotate the guard with a rule id.</summary>
  [Test]
  public void Unwitnessed_capabilities_match_the_acknowledged_backlog()
  {
    var live = Live.Value.Unwitnessed.Select(m => m.Live.Describe()).OrderBy(x => x, StringComparer.Ordinal).ToList();
    var acknowledged = AcknowledgedUnwitnessed.Keys.OrderBy(x => x, StringComparer.Ordinal).ToList();

    var newlyUnwitnessed = live.Except(acknowledged, StringComparer.Ordinal).ToList();
    var resolved = acknowledged.Except(live, StringComparer.Ordinal).ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        newlyUnwitnessed,
        Is.Empty,
        "ADR-0004 §2 soundness — the live engine forms rules-defined edges NO gold witnesses (a new "
          + "unwitnessed capability):\n"
          + string.Join("\n", Live.Value.Unwitnessed
            .Where(m => newlyUnwitnessed.Contains(m.Live.Describe()))
            .Select(m => $"  {m.Live.Describe()}  fired in: {string.Join(", ", m.Live.FiringSentinels)}"))
          + "\nAuthor a gold that declares this mechanism (for a subsumption, two golds whose ports the is-a "
          + "lattice relates), or — if it is genuinely backlog — add it to AcknowledgedUnwitnessed with a reason.");
      Assert.That(
        resolved,
        Is.Empty,
        "acknowledged-backlog mechanism(s) are no longer unwitnessed — a gold now witnesses them. Strike "
          + "them from AcknowledgedUnwitnessed:\n  " + string.Join("\n  ", resolved));
    });

    TestContext.Out.WriteLine(
      $"soundness: {Live.Value.Mechanisms.Count - Live.Value.Unwitnessed.Count} of {Live.Value.Mechanisms.Count} "
        + $"live rules-mechanisms witnessed; {AcknowledgedUnwitnessed.Count} acknowledged backlog:");
    foreach (var (k, why) in AcknowledgedUnwitnessed)
      TestContext.Out.WriteLine($"  [BACKLOG] {k} — {why}");
  }

  // ── the split, reported (deliverable #3 / #6) ──────────────────────────────────────────────────────

  /// <summary>Reports the witnessed/unwitnessed split over the live rules-mechanisms and the 29 rollup
  /// rules, and the number of golds the soundness leg leans on (the hand-evidence dependence, as a number).
  /// Not a pass/fail beyond the non-vacuity floors — the pass/fail is the gate above; this is the census the
  /// ADR wants legible.</summary>
  [Test]
  public void Report_the_witness_split()
  {
    var r = Live.Value;
    TestContext.Out.WriteLine("── live rules-mechanisms (structure ↔ structure) ──");
    foreach (var m in r.Mechanisms)
      TestContext.Out.WriteLine($"  [{m.Kind}] {m.Live.Describe()}  ×{m.Live.Count}  witnesses: {string.Join(", ", m.WitnessingGolds.Take(4))}");
    TestContext.Out.WriteLine(
      $"  → {r.StemExact.Count} stem-exact, {r.CoFiringOnly.Count} co-firing-only, {r.Unwitnessed.Count} unwitnessed");

    TestContext.Out.WriteLine("\n── the 29 rollup rules ──");
    foreach (var section in new[] { "bridges", "match_policy", "polarity", "guards" })
    {
      var rows = r.Rules.Where(x => x.Rule.Section == section).ToList();
      var flowN = rows.Count(x => x.FlowRealized);
      TestContext.Out.WriteLine($"  {section} ({rows.Count}): {flowN} flow-realized, {rows.Count} witnessed-by-gold, {rows.Count(x => x.LiveExercised)} live-exercised");
      foreach (var x in rows)
        TestContext.Out.WriteLine(
          $"    {x.Rule.RuleId}: witnesses={x.Rule.Witnesses.Count}"
            + (x.FlowRealized ? " [flow-realized]" : "")
            + (x.LiveExercised ? " [live]" : "")
            + (x.Rule.FromStem is { } fs ? $"  ({fs}→{x.Rule.ToStem})" : ""));
    }

    var handGolds = r.Mechanisms.SelectMany(m => m.WitnessingGolds).Concat(r.Rules.SelectMany(x => x.Rule.Witnesses))
      .Distinct(StringComparer.Ordinal).Count();
    TestContext.Out.WriteLine($"\nhand-evidence dependence: {handGolds} distinct interaction golds witness the residual layer.");

    // The rollup rules whose structural signature the live Materialize flow does NOT realize — reported,
    // not gated (they are operator-layer or cycle-layer, realized off the flow-edge path).
    TestContext.Out.WriteLine(
      "\nnot flow-realized (operator-layer or cycle-layer — realized off Materialize's flow edges):\n  "
        + string.Join("\n  ", r.NotFlowRealized.Select(x => $"{x.Rule.Section}:{x.Rule.RuleId}")));

    // Every rollup rule has a witnessing gold today (deliverable #3: no guard without a witness). Asserted
    // so a future guard added without a witness fails here, named.
    Assert.That(
      r.UnwitnessedRules,
      Is.Empty,
      "rollup rule(s) with NO witnessing gold (ADR-0004 §2 — unevidenced residual behavior):\n"
        + string.Join("\n", r.UnwitnessedRules.Select(x => $"  {x.Rule.Section}:{x.Rule.RuleId}")));
  }

  // ── non-vacuity proofs: the gate has teeth (deliverable #4) ────────────────────────────────────────

  /// <summary>
  /// RED experiment 1 — <b>an engine capability no gold declares fails the gate.</b> We take the REAL live
  /// inventory and inject one synthetic rules-mechanism (a fabricated arm on stems no gold mentions, firing
  /// only in a fabricated sentinel that declares nothing), then re-run the join against the real golds. The
  /// gate must flag it unwitnessed — proving the pass above is not the empty-set passing.
  /// </summary>
  [Test]
  public void Red_a_fabricated_capability_is_unwitnessed()
  {
    var inv = LiveInventory.Value;
    var rogue = new EdgeMechanismInventory.LiveMechanism(
      EdgeMechanism.FlowArm, PortFlowMatcher.FlowArm.LifeToTrigger, "wormhole", "nowhere",
      EdgeProvenance.RulesDefined, 1, ["synthetic-rogue-sentinel"]);
    var injected = inv with { Mechanisms = [.. inv.Mechanisms, rogue] };

    var (connectors, goldsWithRules) = EdgeMechanismBijection.LoadGoldConnectors(Root);
    var rollup = EdgeMechanismBijection.LoadRollupRules(Root);
    var result = EdgeMechanismBijection.Join(
      injected, connectors, rollup, goldsWithRules, EdgeMechanismBijection.ProjectedSentinels(inv));

    Assert.That(
      result.Unwitnessed.Select(m => m.Live.FromStem),
      Does.Contain("wormhole"),
      "the injected rogue capability must be flagged unwitnessed");
    var rogueVerdict = result.Unwitnessed.Single(m => m.Live.FromStem == "wormhole");
    TestContext.Out.WriteLine($"RED as expected: {rogueVerdict.Describe()}");
  }

  /// <summary>
  /// RED experiment 2 — <b>removing the witnessing gold(s) turns a real mechanism unwitnessed.</b> The dice
  /// arm (<c>FlowArm/DiceToTrigger [dice → dice]</c>) fires live only in Pair o' Dice Lost × Brazen Dwarf,
  /// and only that gold declares a <c>dice → dice</c> rules-connector. Delete every gold that witnesses it —
  /// the live analogue of removing the gold from the loop — and re-run: the arm must go unwitnessed. This
  /// proves the pass is not stem-matching against a set that can never shrink.
  /// </summary>
  [Test]
  public void Red_removing_the_witnessing_golds_makes_the_mechanism_unwitnessed()
  {
    var inv = LiveInventory.Value;
    var dice = inv.Mechanisms.SingleOrDefault(m =>
      m.Arm == PortFlowMatcher.FlowArm.DiceToTrigger && m.FromStem == "dice" && m.ToStem == "dice");
    Assert.That(dice, Is.Not.Null, "expected a live DiceToTrigger [dice→dice] mechanism to exist");

    var (connectors, goldsWithRules) = EdgeMechanismBijection.LoadGoldConnectors(Root);
    // Every gold that could witness the dice arm: its firing sentinels + any gold declaring dice→dice.
    var witnesses = dice!.FiringSentinels
      .Concat(connectors.Where(c => c.FromStem == "dice" && c.ToStem == "dice").Select(c => c.GoldId))
      .ToHashSet(StringComparer.Ordinal);
    Assert.That(witnesses, Is.Not.Empty);

    var withoutGolds = goldsWithRules.Where(g => !witnesses.Contains(g)).ToHashSet(StringComparer.Ordinal);
    var connectorsWithout = connectors.Where(c => !witnesses.Contains(c.GoldId)).ToList();
    var rollup = EdgeMechanismBijection.LoadRollupRules(Root);

    var result = EdgeMechanismBijection.Join(
      inv, connectorsWithout, rollup, withoutGolds, EdgeMechanismBijection.ProjectedSentinels(inv));

    Assert.That(
      result.Unwitnessed.Select(m => m.Live.Describe()),
      Does.Contain("FlowArm/DiceToTrigger [dice → dice]"),
      "with its witnessing gold(s) deleted, the dice arm is an unwitnessed capability");
    TestContext.Out.WriteLine("RED as expected: removing " + string.Join(", ", witnesses) + " orphans "
      + string.Join(", ", result.Unwitnessed.Select(m => m.Describe())));
  }

  /// <summary>The zero-hand-authoring proof for the mechanism side: the join injects nothing. Feed it a
  /// synthetic live mechanism plus a gold connector at the SAME stems, and the mechanism is witnessed purely
  /// because the structural stems coincide — no rule id, no gold id, was consulted.</summary>
  [Test]
  public void Witness_is_purely_structural()
  {
    var inv = new EdgeMechanismInventory.Inventory(
      Mechanisms:
      [
        new(EdgeMechanism.FlowArm, PortFlowMatcher.FlowArm.SacrificeDeathToTrigger, "alpha:x", "alpha:x",
          EdgeProvenance.RulesDefined, 1, ["g1"]),
        new(EdgeMechanism.FlowArm, PortFlowMatcher.FlowArm.TokenToSac, "beta:y", "gamma:z",
          EdgeProvenance.RulesDefined, 1, ["g2"]),
      ],
      SentinelsRun: 2, SentinelsProjected: 2, EdgesFormed: 2);

    var golds = new List<EdgeMechanismBijection.GoldConnector>
    {
      new("g1", "alpha:x", "alpha:x", "subsumption", null), // stem-exact witness for the first
      // g2 declares SOME rules-edge but at different stems → co-firing witness only for the second
      new("g2", "delta:w", "delta:w", "bridge", "bridge:unrelated"),
    };
    var result = EdgeMechanismBijection.Join(
      inv, golds, [], new HashSet<string> { "g1", "g2" }, new HashSet<string> { "g1", "g2" });

    Assert.Multiple(() =>
    {
      Assert.That(result.StemExact.Select(m => m.Live.Arm), Is.EquivalentTo(new[] { (PortFlowMatcher.FlowArm?)PortFlowMatcher.FlowArm.SacrificeDeathToTrigger }));
      Assert.That(result.CoFiringOnly.Select(m => m.Live.Arm), Is.EquivalentTo(new[] { (PortFlowMatcher.FlowArm?)PortFlowMatcher.FlowArm.TokenToSac }));
      Assert.That(result.Unwitnessed, Is.Empty);
    });
  }
}
