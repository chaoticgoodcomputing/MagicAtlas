namespace MagicAST.Interaction.Tests;

using MagicAtlas.Ast.Tests.Flows.DerivedBacklog;

/// <summary>
/// Exhaustiveness invariant for <see cref="PortWalk"/> (alignment initiative 03 #1; de-ratcheted
/// 2026-06-16; <b>re-pointed at the derived backlog 2026-07-22, ADR-0004 §2 / issue #32</b>). Every AST
/// discriminator that PortWalk dispatches on — every <c>EffectType</c>, <c>CostType</c>, trigger
/// <c>Event</c>, and restriction — must be EITHER semantically projected (declared in
/// <see cref="PortWalkProjection"/>) OR <b>accounted for</b> by the derived backlog: it is in the backlog
/// (an unserved projection with no gold), a decision (an unserved projection an asserted-absence gold
/// removes), or an excluded not-a-port-candidate (a parse-ledger escape hatch). A discriminator that is
/// none of those fails loudly.
///
/// <para><b>What changed.</b> This gate used to read a hand-maintained named whitelist
/// (<c>libs/mast-interaction/known-coarse-projections.json</c>) — every non-projected discriminator carried
/// a prose "reason" that nothing checked as the corpus/parser moved under it (ADR-0004's Context table: the
/// whitelist "edited three times in one session; nothing checks whether the prose is still true"). That
/// file is <b>deleted</b>. The blind-spot set is now DERIVED — <c>all discriminators − PortWalkProjection −
/// asserted-unarmable(golds)</c> is exactly ADR-0004 §2's backlog — and the loud signal survives without a
/// name list: a new discriminator that is neither projected nor an asserted-absence gold simply appears in
/// the backlog, and this gate proves the accounting is complete and non-vacuous.</para>
///
/// <para><b>Stateless by construction</b>, like the cross-track join gates: it re-runs the pure
/// <see cref="BacklogDerivation.Compute"/> over the live schema + engine + golds, never reading the
/// gitignored <c>_08_Reporting/derived-backlog.json</c> the flow writes — a gate that read the artifact it
/// is meant to check would verify the derivation against itself.</para>
/// </summary>
[TestFixture]
public class PortWalkExhaustivenessTests
{
  private static readonly string Root = BacklogSources.RepoRoot();

  private static readonly Lazy<Live> Computed = new(() =>
  {
    var all = BacklogDerivation.AllByDimension();
    var served = BacklogDerivation.ServedByDimension();
    var decisionSources = BacklogSources.LoadAssertedUnarmable(BacklogSources.GoldsDir(Root), all);
    var result = BacklogDerivation.Compute(
      all,
      served,
      decisionSources.Select(d => d.Term).ToHashSet(),
      BacklogDerivation.NotPortCandidates
    );
    return new Live(all, served, decisionSources, result);
  });

  private sealed record Live(
    IReadOnlyDictionary<string, IReadOnlySet<string>> All,
    IReadOnlyDictionary<string, IReadOnlySet<string>> Served,
    IReadOnlyList<BacklogSources.DecisionSource> DecisionSources,
    BacklogDerivation.BacklogResult Result
  );

  // ── THE GATE: every unprojected discriminator is accounted for ────────────────────────────────────

  /// <summary>
  /// The invariant, re-pointed at the backlog: for every dispatch dimension, the unserved discriminators
  /// (<c>all − served</c>) are EXACTLY the union of the derived backlog, the decisions, and the excluded
  /// not-a-port-candidates. Nothing unprojected falls through unaccounted, and nothing is invented — the
  /// analog of the old "every discriminator is projected or explicitly whitelisted", now derived.
  /// </summary>
  [Test]
  public void Every_unprojected_discriminator_is_accounted_for()
  {
    var live = Computed.Value;
    var accounted = live
      .Result.Backlog.Concat(live.Result.Decisions)
      .Concat(live.Result.Excluded)
      .ToHashSet();

    var unaccounted = new List<string>();
    foreach (var (dim, all) in live.All)
    {
      var served = live.Served.GetValueOrDefault(dim, new HashSet<string>(StringComparer.Ordinal));
      foreach (var d in all.Where(x => !served.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
        if (!accounted.Contains(new BacklogDerivation.Term(dim, d)))
          unaccounted.Add($"[{dim}] {d}");
    }

    Assert.That(
      unaccounted,
      Is.Empty,
      "ADR-0004 §2: these unprojected discriminators are in neither the backlog, the decisions, nor the "
        + "not-a-port-candidate exclusions — the derivation dropped them:\n  "
        + string.Join("\n  ", unaccounted)
    );
  }

  /// <summary>Declared-projected entries must be real discriminators (the typo/stale guard, retained):
  /// <c>served ⊆ all</c>, or PortWalkProjection names a discriminator that no longer exists.</summary>
  [Test]
  public void Projected_entries_are_real_discriminators()
  {
    var live = Computed.Value;
    var stale = new List<string>();
    foreach (var (dim, served) in live.Served)
    {
      var all = live.All.GetValueOrDefault(dim, new HashSet<string>(StringComparer.Ordinal));
      foreach (var p in served.Where(x => !all.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
        stale.Add($"[{dim}] PortWalkProjection declares \"{p}\" but no such discriminator exists (typo/stale).");
    }

    Assert.That(stale, Is.Empty, string.Join("\n", stale));
  }

  /// <summary>
  /// Every decision (asserted-unarmable subtrahend member) must trace to a gold — "every asserted-unarmable
  /// one has a gold" (Appendix C, handover step 2). A decision with no gold, or a gold asserting
  /// <c>no_arm</c> over a discriminator that is SERVED or does not exist (a dangling decision), is a stale
  /// or contradictory claim and fails — the analog of the old "whitelisted but now projected" / "stale
  /// entry" checks.
  /// </summary>
  [Test]
  public void Every_decision_traces_to_a_gold_and_none_dangle()
  {
    var live = Computed.Value;
    var byTerm = live.DecisionSources.ToDictionary(d => d.Term, d => d);

    Assert.Multiple(() =>
    {
      Assert.That(
        live.Result.DanglingDecisions,
        Is.Empty,
        "a gold asserts no_arm over a discriminator that is served or does not exist:\n  "
          + string.Join("\n  ", live.Result.DanglingDecisions.Select(t => t.ToString()))
      );
      foreach (var d in live.Result.Decisions)
        Assert.That(
          byTerm.ContainsKey(d),
          Is.True,
          $"decision {d} has no witnessing gold — a decision must be an asserted-absence gold, never a bare exclusion"
        );
    });
  }

  // ── non-vacuity ───────────────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// The join must not pass because a side came back empty. A corpus-less checkout still has the full schema
  /// (projected), engine (served) and committed golds (the subtrahend), so all three terms are live here.
  /// The known decision <c>anyNumberInDeck</c> must be present and trace to its gold — the standing proof
  /// that the subtrahend is wired to real golds, not baked.
  /// </summary>
  [Test]
  public void Backlog_is_non_vacuous()
  {
    var live = Computed.Value;

    Assert.Multiple(() =>
    {
      Assert.That(live.All.Values.Sum(s => s.Count), Is.GreaterThan(0), "no discriminators were reflected");
      Assert.That(live.Served.Values.Sum(s => s.Count), Is.GreaterThan(0), "no served discriminators were reflected");
      Assert.That(live.Result.Backlog, Is.Not.Empty, "the derived backlog is empty — the subtrahend or served set swallowed everything");
      Assert.That(live.DecisionSources, Is.Not.Empty, "no asserted-unarmable gold was read — the subtrahend is not wired to the golds");

      var anyNumber = new BacklogDerivation.Term("effectType", "anyNumberInDeck");
      Assert.That(
        live.Result.Decisions,
        Does.Contain(anyNumber),
        "anyNumberInDeck is not a decision — the rat-colony asserted-absence gold is not removing its port"
      );
      Assert.That(
        live.Result.Backlog,
        Does.Not.Contain(anyNumber),
        "anyNumberInDeck is in the backlog despite its gold — the subtrahend is not live"
      );
      Assert.That(
        live.DecisionSources.Single(d => d.Term == anyNumber).Gold,
        Is.EqualTo("rat-colony-deck-construction-terminal"),
        "the anyNumberInDeck decision does not trace to the rat-colony gold"
      );
    });

    TestContext.WriteLine(
      $"derived backlog: {live.Result.Backlog.Count} discriminators, {live.Result.Decisions.Count} decisions, "
        + $"{live.Result.Excluded.Count} excluded (not-a-port-candidate) over {live.DecisionSources.Count} gold-sourced probes"
    );
    foreach (var (dim, c) in live.Result.ByDimension.OrderBy(kv => kv.Key, StringComparer.Ordinal))
      TestContext.WriteLine($"  {dim}: all={c.All} served={c.Served} backlog={c.Backlog} decisions={c.Decisions} excluded={c.Excluded}");
  }

  // ── self-tests of the pure derivation (synthetic, no real schema) — the teeth ─────────────────────

  private static IReadOnlyDictionary<string, IReadOnlySet<string>> Dim(string dim, params string[] xs) =>
    new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
    {
      [dim] = xs.ToHashSet(StringComparer.Ordinal),
    };

  [Test]
  public void Unprojected_with_no_gold_is_backlog()
  {
    var result = BacklogDerivation.Compute(
      Dim("effectType", "createToken", "brandNewEffect"),
      Dim("effectType", "createToken"),
      assertedUnarmable: new HashSet<BacklogDerivation.Term>(),
      notPortCandidates: new HashSet<BacklogDerivation.Term>()
    );
    Assert.Multiple(() =>
    {
      Assert.That(result.Backlog, Does.Contain(new BacklogDerivation.Term("effectType", "brandNewEffect")));
      Assert.That(result.Decisions, Is.Empty);
    });
  }

  [Test]
  public void Unprojected_with_a_gold_is_a_decision_not_backlog()
  {
    var decided = new BacklogDerivation.Term("effectType", "brandNewEffect");
    var result = BacklogDerivation.Compute(
      Dim("effectType", "createToken", "brandNewEffect"),
      Dim("effectType", "createToken"),
      assertedUnarmable: new HashSet<BacklogDerivation.Term> { decided },
      notPortCandidates: new HashSet<BacklogDerivation.Term>()
    );
    Assert.Multiple(() =>
    {
      Assert.That(result.Decisions, Does.Contain(decided), "an unserved projection WITH a gold is a decision");
      Assert.That(result.Backlog, Does.Not.Contain(decided), "…and therefore not backlog — the whole-job distinction");
    });
  }

  /// <summary>An EMPTY subtrahend is the normal case (ADR-0004 §2 / #28's strong-prior-backlog), not an
  /// error: the same discriminator that was a decision above is backlog with no gold, and the derivation
  /// neither throws nor warns.</summary>
  [Test]
  public void Empty_subtrahend_is_the_normal_case_not_an_error()
  {
    var result = BacklogDerivation.Compute(
      Dim("effectType", "createToken", "brandNewEffect"),
      Dim("effectType", "createToken"),
      assertedUnarmable: new HashSet<BacklogDerivation.Term>(), // empty
      notPortCandidates: new HashSet<BacklogDerivation.Term>()
    );
    Assert.Multiple(() =>
    {
      Assert.That(result.Backlog, Does.Contain(new BacklogDerivation.Term("effectType", "brandNewEffect")));
      Assert.That(result.Decisions, Is.Empty);
      Assert.That(result.DanglingDecisions, Is.Empty);
    });
  }

  [Test]
  public void Not_a_port_candidate_is_excluded_not_backlog()
  {
    var hatch = new BacklogDerivation.Term("effectType", "unparsed");
    var result = BacklogDerivation.Compute(
      Dim("effectType", "createToken", "unparsed"),
      Dim("effectType", "createToken"),
      assertedUnarmable: new HashSet<BacklogDerivation.Term>(),
      notPortCandidates: new HashSet<BacklogDerivation.Term> { hatch }
    );
    Assert.Multiple(() =>
    {
      Assert.That(result.Excluded, Does.Contain(hatch), "a parse-ledger escape hatch is excluded, not backlog");
      Assert.That(result.Backlog, Does.Not.Contain(hatch));
    });
  }

  /// <summary>Teeth on the dangling case: a gold asserting no_arm over a SERVED discriminator (someone armed
  /// the port under it) must surface as dangling — the analog of the old "whitelisted but now projected".</summary>
  [Test]
  public void A_gold_asserting_over_a_served_discriminator_dangles()
  {
    var served = new BacklogDerivation.Term("effectType", "createToken");
    var result = BacklogDerivation.Compute(
      Dim("effectType", "createToken"),
      Dim("effectType", "createToken"),
      assertedUnarmable: new HashSet<BacklogDerivation.Term> { served },
      notPortCandidates: new HashSet<BacklogDerivation.Term>()
    );
    Assert.That(result.DanglingDecisions, Does.Contain(served), "a decision over a served port is stale/contradictory");
  }
}
