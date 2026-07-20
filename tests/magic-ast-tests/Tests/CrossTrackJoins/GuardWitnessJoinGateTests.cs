namespace MagicAST.Tests.Tests.CrossTrackJoins;

using MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

/// <summary>
/// The <b>ADR-0004 §4 join-2 GATE</b> — <c>gold <c>declares</c> → rollup rule → engine guard</c>: §2's
/// bijection, soundness half, materialized.
/// </summary>
/// <remarks>
/// <para><b>No registry.</b> ADR-0003 §6 states "every guard is registered with its witnessing golds" and
/// never gated it; the danger in gating it was that "registered" would become a hand-authored table — the
/// exact species of artifact ADR-0004 exists to kill. It does not: the gold schema already carries
/// <c>declares: { polarity, match_policy, guards, bridges }</c>, so <c>witnesses(rule)</c> is simply the
/// set of gold ids that declared it. <see cref="Map_is_derived_purely_from_declares"/> proves that
/// mechanically, by feeding the joiner a synthetic gold set and asserting the output contains exactly
/// what went in — no entry from any other source.</para>
///
/// <para><b>Three legs, all derived.</b> The map (from <c>declares</c>) is checked against the committed
/// rollup's own witness attribution — a genuine cross-artifact check that catches a stale rollup — and
/// against a literal scan of the engine sources for each rule id. The third leg is REPORTED, not gated:
/// no engine source currently names any rule id, so the rule→code half of the bijection cannot be checked
/// yet. Closing it is issue #34's job, and this gate's output is the input it reads.</para>
///
/// <para><b>Stateless by construction</b>, like the sibling quarantine gate: it re-runs the pure joiner
/// over the live committed artifacts instead of reading the gitignored
/// <c>_08_Reporting/guard-witness-join.json</c>.</para>
/// </remarks>
[TestFixture]
public class GuardWitnessJoinGateTests
{
  private static readonly string Root = CrossTrackSources.RepoRoot();

  private static readonly Lazy<CrossTrackJoiner.GuardWitnessJoin> Live = new(() =>
  {
    var (declared, citations, goldsScanned) = CrossTrackSources.LoadGoldDeclarations(Root);
    var rollup = CrossTrackSources.LoadRollupRules(Root);
    var (codeRefs, filesScanned) = CrossTrackSources.ScanEngineSources(
      Root,
      declared.Select(d => d.RuleId).Concat(rollup.Select(r => r.RuleId))
    );
    return CrossTrackJoiner.JoinGuardsToWitnesses(declared, citations, rollup, codeRefs, goldsScanned, filesScanned);
  });

  // ── soundness ───────────────────────────────────────────────────────────────────────────────────

  /// <summary>THE GATE (§2.1, soundness): <c>∀ r ∈ R : witnesses(r) ∩ G ≠ ∅</c>. No residual rule exists
  /// without a gold that witnesses it.</summary>
  [Test]
  public void Every_rule_has_a_witnessing_gold()
  {
    var unwitnessed = Live.Value.Unwitnessed;

    Assert.That(
      unwitnessed,
      Is.Empty,
      "ADR-0004 §2 soundness: these residual rules have no witnessing gold —\n"
        + string.Join("\n", unwitnessed.Select(r => $"  {r.Section}:{r.RuleId}"))
        + "\nA rule with no witness is unevidenced behavior. Author a gold that declares it, or delete it."
    );
  }

  /// <summary>The middle leg: a rule the committed rollup carries that no gold declares. Structurally the
  /// same soundness violation, seen from the artifact rather than from the map — and the way a
  /// hand-edited rollup would surface.</summary>
  [Test]
  public void No_rollup_rule_is_undeclared_by_the_golds()
  {
    Assert.That(
      Live.Value.RollupRulesMissingFromGolds,
      Is.Empty,
      "the committed rollup carries rule(s) no gold declares:\n"
        + string.Join("\n", Live.Value.RollupRulesMissingFromGolds.Select(r => "  " + r))
        + "\nThe rollup is generated from the golds' declares — a rule present in one and absent from the "
        + "other means the committed rollup was hand-edited or is stale. Regenerate: dotnet run -- --flow "
        + "InteractionRollup."
    );
  }

  /// <summary>…and the converse: a rule the golds declare that the committed rollup does not carry. Same
  /// staleness, opposite direction.</summary>
  [Test]
  public void No_declared_rule_is_missing_from_the_rollup()
  {
    Assert.That(
      Live.Value.GoldRulesMissingFromRollup,
      Is.Empty,
      "gold(s) declare rule(s) the committed rollup does not carry:\n"
        + string.Join("\n", Live.Value.GoldRulesMissingFromRollup.Select(r => "  " + r))
        + "\nRegenerate the rollup: dotnet run -- --flow InteractionRollup."
    );
  }

  /// <summary>The witness ATTRIBUTION itself must agree between the two artifacts — not merely the rule
  /// set. This is where a stale <c>.cited</c> twin shows up, and per ADR-0004 §3 the <c>.cited</c> diff is
  /// "the highest-value of the four: it is where witness attribution changes surface."</summary>
  [Test]
  public void Rollup_witness_attribution_matches_the_golds()
  {
    Assert.That(
      Live.Value.Disagreements,
      Is.Empty,
      "the committed rollup's witnesses disagree with the golds' own declares:\n"
        + string.Join(
          "\n",
          Live.Value.Disagreements.Select(d =>
            $"  {d.RuleId}: golds-only [{string.Join(", ", d.InGoldsOnly)}] rollup-only [{string.Join(", ", d.InRollupOnly)}]"
          )
        )
        + "\nRegenerate the rollup: dotnet run -- --flow InteractionRollup."
    );
  }

  /// <summary>Every rule an edge cites must exist in the map. A dangling citation is a gold claiming a
  /// mechanism nothing defines.</summary>
  [Test]
  public void No_edge_cites_an_undeclared_rule()
  {
    Assert.That(
      Live.Value.DanglingCitations,
      Is.Empty,
      "gold edge(s) cite a rule no gold declares:\n"
        + string.Join("\n", Live.Value.DanglingCitations.Select(c => $"  {c.GoldId}#{c.EdgeId} → {c.RuleId}"))
    );
  }

  // ── non-vacuity ─────────────────────────────────────────────────────────────────────────────────

  /// <summary>Every leg must have actually loaded. "∀ r ∈ ∅" is true and proves nothing — a join that
  /// passed because one side came back empty is the failure mode this ADR exists to remove.</summary>
  [Test]
  public void Join_is_non_vacuous()
  {
    Assert.Multiple(() =>
    {
      Assert.That(Live.Value.GoldsScanned, Is.GreaterThan(0), "no interaction golds were read");
      Assert.That(Live.Value.GoldsDeclaringRules, Is.GreaterThan(0), "no gold declared any rule");
      Assert.That(Live.Value.Rules, Is.Not.Empty, "the guard→witness map is empty");
      Assert.That(Live.Value.RollupRuleCount, Is.GreaterThan(0), "the committed rollup carried no rules");
      Assert.That(Live.Value.SourceFilesScanned, Is.GreaterThan(0), "the engine source scan found no files");
      Assert.That(Live.Value.Vacuous, Is.False);
      Assert.That(
        Live.Value.Rules.Where(r => r.Section == "guards"),
        Is.Not.Empty,
        "no GUARD rule is in the map — the section this join is named for went missing"
      );
      Assert.That(
        Live.Value.Rules.Where(r => r.Section == "bridges"),
        Is.Not.Empty,
        "no BRIDGE rule is in the map"
      );
    });

    TestContext.WriteLine(
      $"guard→witness map: {Live.Value.Rules.Count} rules from {Live.Value.GoldsDeclaringRules}/{Live.Value.GoldsScanned} golds; "
        + $"rollup carries {Live.Value.RollupRuleCount}; engine sources scanned {Live.Value.SourceFilesScanned}; "
        + $"code-unlinked {Live.Value.CodeUnlinked.Count} (reported, not gated — issue #34)"
    );
    foreach (var r in Live.Value.Rules.Where(r => r.Section is "guards"))
      TestContext.WriteLine($"  {r.RuleId} [{r.Status}] witnesses: {string.Join(", ", r.Witnesses)}");
  }

  // ── the zero-hand-authoring proof ───────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>Proof that the map has no hand-authored entries.</b> Feed the joiner a synthetic gold set whose
  /// declares mention rules that exist nowhere in this repository, with an empty rollup and an empty code
  /// scan. The output must be <i>exactly</i> those rules with <i>exactly</i> those witnesses: nothing
  /// injected, nothing defaulted, nothing remembered. A table anywhere in the derivation would show up
  /// here as an extra row.
  /// </summary>
  [Test]
  public void Map_is_derived_purely_from_declares()
  {
    var declared = new List<CrossTrackJoiner.DeclaredRule>
    {
      new("gold-alpha", "guards", "guard:synthetic-alpha", "code", "alpha", ["100.1"], JudgePassed: false),
      new("gold-beta", "guards", "guard:synthetic-alpha", "code", "alpha", ["100.1"], JudgePassed: false),
      new("gold-beta", "bridges", "bridge:synthetic-beta", null, "beta", [], JudgePassed: true),
    };

    var result = CrossTrackJoiner.JoinGuardsToWitnesses(
      declared,
      citations: [],
      rollup: [],
      codeReferencesByRuleId: new Dictionary<string, IReadOnlyList<CrossTrackJoiner.CodeReference>>(),
      goldsScanned: 2,
      sourceFilesScanned: 0
    );

    Assert.Multiple(() =>
    {
      Assert.That(
        result.Rules.Select(r => r.RuleId),
        Is.EqualTo(new[] { "bridge:synthetic-beta", "guard:synthetic-alpha" }),
        "the map contains exactly the declared rules — no more, no fewer"
      );
      Assert.That(
        result.Rules.Single(r => r.RuleId == "guard:synthetic-alpha").Witnesses,
        Is.EqualTo(new[] { "gold-alpha", "gold-beta" }),
        "witnesses are exactly the declaring golds"
      );
      Assert.That(
        result.Rules.Single(r => r.RuleId == "guard:synthetic-alpha").Status,
        Is.EqualTo("corroborated"),
        "two witnesses, none judge-PASSed"
      );
      Assert.That(
        result.Rules.Single(r => r.RuleId == "bridge:synthetic-beta").Status,
        Is.EqualTo("confirmed"),
        "one judge-PASSed witness"
      );
      Assert.That(result.Unwitnessed, Is.Empty, "a rule derived from declares always has its declarer");
    });
  }

  /// <summary>
  /// The falsification of the soundness gate: a rollup rule that no gold declares must be caught. Without
  /// this, <see cref="No_rollup_rule_is_undeclared_by_the_golds"/> could be green because the delta is
  /// never computed.
  /// </summary>
  [Test]
  public void Synthetic_unwitnessed_rollup_rule_fails_the_join()
  {
    var result = CrossTrackJoiner.JoinGuardsToWitnesses(
      declared: [new("gold-alpha", "guards", "guard:synthetic-alpha", "code", null, [], JudgePassed: true)],
      citations: [new("gold-alpha", "E1", "bridge:never-declared", "GREEN")],
      rollup:
      [
        new("guards", "guard:synthetic-alpha", "confirmed", ["gold-alpha"]),
        new("guards", "guard:appeared-from-nowhere", "confirmed", ["gold-alpha"]),
      ],
      codeReferencesByRuleId: new Dictionary<string, IReadOnlyList<CrossTrackJoiner.CodeReference>>(),
      goldsScanned: 1,
      sourceFilesScanned: 1
    );

    Assert.Multiple(() =>
    {
      Assert.That(
        result.RollupRulesMissingFromGolds,
        Is.EqualTo(new[] { "guard:appeared-from-nowhere" }),
        "a rollup rule no gold declares is an unwitnessed rule"
      );
      Assert.That(
        result.DanglingCitations.Select(c => c.RuleId),
        Is.EqualTo(new[] { "bridge:never-declared" }),
        "an edge citing an undeclared rule is caught"
      );
    });
  }

  /// <summary>
  /// The falsification of the witness-attribution gate: a rollup that credits a different gold than the
  /// declares do must be caught. This is the stale-<c>.cited</c> case.
  /// </summary>
  [Test]
  public void Synthetic_stale_rollup_witness_fails_the_join()
  {
    var result = CrossTrackJoiner.JoinGuardsToWitnesses(
      declared:
      [
        new("gold-alpha", "guards", "guard:synthetic-alpha", "code", null, [], JudgePassed: true),
        new("gold-beta", "guards", "guard:synthetic-alpha", "code", null, [], JudgePassed: true),
      ],
      citations: [],
      rollup: [new("guards", "guard:synthetic-alpha", "confirmed", ["gold-alpha", "gold-gone"])],
      codeReferencesByRuleId: new Dictionary<string, IReadOnlyList<CrossTrackJoiner.CodeReference>>(),
      goldsScanned: 2,
      sourceFilesScanned: 1
    );

    Assert.That(result.Disagreements, Has.Count.EqualTo(1));
    Assert.Multiple(() =>
    {
      Assert.That(result.Disagreements[0].InGoldsOnly, Is.EqualTo(new[] { "gold-beta" }));
      Assert.That(result.Disagreements[0].InRollupOnly, Is.EqualTo(new[] { "gold-gone" }));
    });
  }
}
