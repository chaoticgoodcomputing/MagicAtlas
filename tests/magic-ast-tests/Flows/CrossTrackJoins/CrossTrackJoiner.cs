namespace MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

/// <summary>
/// The <b>ADR-0004 §4 cross-track joins</b>, as pure functions over value inputs.
///
/// <para>§4's thesis, stated on the Suture Priest incident: <i>neither regeneration nor gitignoring
/// catches a disconnected artifact, because both verify a fact against itself. Only a materialized join
/// verifies a fact against a different track's claims.</i> Two joins live here:</para>
/// <list type="number">
///   <item><b>quarantined-oracle-text → gold → shipped combo tier</b>
///     (<see cref="JoinQuarantineToTiers"/>) — the Parse track's fidelity quarantine crossed with the
///     Interaction track's pinned combo tiers. A GREEN pin resting on quarantined text is a violation.</item>
///   <item><b>gold <c>declares</c> → rollup rule → engine guard</b>
///     (<see cref="JoinGuardsToWitnesses"/>) — §2's bijection, soundness half. The guard→witness map is
///     <b>computed from the golds' own <c>declares</c> blocks</b>; there is no registry in this file and
///     no rule id is written anywhere in this assembly.</item>
/// </list>
///
/// <para><b>Pure by construction, for two reasons.</b> Flowthru steps call these to produce the
/// <c>_08_Reporting</c> artifacts; the NUnit gates call them over the live committed artifacts <i>and</i>
/// over hermetic reconstructed inputs (the Suture-Priest shape), which is what makes "this join
/// demonstrably fails on the historical incident" an executable claim rather than a story. Nothing here
/// touches the filesystem — see <see cref="CrossTrackSources"/> for the loaders.</para>
///
/// <para><b>Vacuity is the failure mode this whole ADR exists to remove</b>, so every result type carries
/// its input sizes and a <c>Vacuous</c> predicate: a join that passed because one side came back empty is
/// reported as red, never as green.</para>
/// </summary>
public static class CrossTrackJoiner
{
  // ────────────────────────────────────────────────────────────────────────────────────────────────
  // Join 1 — quarantined oracle text → gold → shipped combo tier
  // ────────────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>One entry of the Parse track's <c>oracle-text-quarantine.json</c>: a gold fixture whose
  /// <c>Input.OracleText</c> is known to drift from its authoritative source.</summary>
  public sealed record QuarantinedFixture(string Fixture, string Tag, string Reason);

  /// <summary>One pin of the Interaction track's <c>combo-expected-tiers.json</c>.</summary>
  public sealed record ComboPin(string Id, string Tier, IReadOnlyList<string> Cards);

  /// <summary>A materialized crossing: pin <see cref="ComboId"/> (tier <see cref="Tier"/>) rests on
  /// <see cref="Card"/>, whose gold fixture <see cref="Fixture"/> is quarantined.</summary>
  public sealed record QuarantineRisk(
    string ComboId,
    string Tier,
    string Card,
    string Fixture,
    string Tag,
    string Reason,
    bool Acknowledged,
    IReadOnlyList<string> InteractionGolds
  )
  {
    /// <summary>A GREEN certification resting on unacknowledged drifted text — the Suture Priest shape.</summary>
    public bool IsViolation => !Acknowledged && Tier.Equals("Green", StringComparison.OrdinalIgnoreCase);

    public string Explain() =>
      $"combo '{ComboId}' is pinned {Tier.ToUpperInvariant()} but rests on '{Card}', whose gold fixture "
      + $"'{Fixture}' is on the oracle-text quarantine (tag '{Tag}': {Reason})"
      + (InteractionGolds.Count > 0
        ? $"; interaction gold(s) naming that card: {string.Join(", ", InteractionGolds)}"
        : "; no interaction gold names that card");
  }

  /// <summary>The join-1 result: every crossing, plus both sides' sizes so emptiness cannot pass as green.</summary>
  public sealed record QuarantineTierJoin(
    IReadOnlyList<QuarantineRisk> Risks,
    int QuarantinedFixtureCount,
    int ResolvedFixtureCount,
    int PinCount,
    int GreenPinCount,
    IReadOnlyList<string> UnresolvedFixtures
  )
  {
    public IReadOnlyList<QuarantineRisk> Violations => [.. Risks.Where(r => r.IsViolation)];

    /// <summary>Either side empty ⇒ the join proves nothing. Reported as a failure, never as a pass.</summary>
    public bool Vacuous => QuarantinedFixtureCount == 0 || PinCount == 0 || GreenPinCount == 0;
  }

  /// <summary>
  /// Crosses the Parse track's quarantine with the Interaction track's pinned tiers, through the
  /// <c>fixture → card name</c> join the Parse track already owns and the <c>card name → combo</c> join
  /// the pins already carry.
  /// </summary>
  /// <param name="quarantine">The Parse track's quarantine entries.</param>
  /// <param name="cardByFixture">Fixture path (<c>SET/CardName</c>) → the card's <c>Input.Name</c>.</param>
  /// <param name="pins">The Interaction track's shipped combo tier pins.</param>
  /// <param name="interactionGoldsByCard">Card name → interaction gold ids naming it (context only).</param>
  /// <param name="acknowledged">
  /// Named, human-reviewed <c>(comboId, fixture)</c> carve-outs — the existing
  /// <c>tools/bench/MagicAtlas.Bench/fidelity-risk-acknowledged.json</c>, reused rather than duplicated.
  /// Matching is on the PAIR, so a new quarantine hit on an already-acknowledged combo is never swallowed.
  /// </param>
  public static QuarantineTierJoin JoinQuarantineToTiers(
    IEnumerable<QuarantinedFixture> quarantine,
    IReadOnlyDictionary<string, string> cardByFixture,
    IEnumerable<ComboPin> pins,
    IReadOnlyDictionary<string, IReadOnlyList<string>> interactionGoldsByCard,
    IReadOnlySet<(string ComboId, string Fixture)> acknowledged
  )
  {
    var quarantined = quarantine.ToList();
    var pinList = pins.ToList();

    // fixture → (card, entry), and the inverse the pins are keyed by.
    var unresolved = new List<string>();
    var byCard = new Dictionary<string, List<QuarantinedFixture>>(StringComparer.Ordinal);
    foreach (var q in quarantined)
    {
      if (!cardByFixture.TryGetValue(q.Fixture, out var card))
      {
        unresolved.Add(q.Fixture);
        continue;
      }
      (byCard.TryGetValue(card, out var list) ? list : byCard[card] = []).Add(q);
    }

    var risks = new List<QuarantineRisk>();
    foreach (var pin in pinList)
    {
      foreach (var card in pin.Cards)
      {
        if (!byCard.TryGetValue(card, out var hits))
          continue;
        foreach (var q in hits)
          risks.Add(
            new QuarantineRisk(
              ComboId: pin.Id,
              Tier: pin.Tier,
              Card: card,
              Fixture: q.Fixture,
              Tag: q.Tag,
              Reason: q.Reason,
              Acknowledged: acknowledged.Contains((pin.Id, q.Fixture)),
              InteractionGolds: interactionGoldsByCard.TryGetValue(card, out var g) ? g : []
            )
          );
      }
    }

    return new QuarantineTierJoin(
      Risks: [.. risks.OrderBy(r => r.ComboId, StringComparer.Ordinal).ThenBy(r => r.Fixture, StringComparer.Ordinal)],
      QuarantinedFixtureCount: quarantined.Count,
      ResolvedFixtureCount: quarantined.Count - unresolved.Count,
      PinCount: pinList.Count,
      GreenPinCount: pinList.Count(p => p.Tier.Equals("Green", StringComparison.OrdinalIgnoreCase)),
      UnresolvedFixtures: [.. unresolved.OrderBy(f => f, StringComparer.Ordinal)]
    );
  }

  // ────────────────────────────────────────────────────────────────────────────────────────────────
  // Join 2 — gold `declares` → rollup rule → engine guard
  // ────────────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>One rule a gold's <c>declares</c> block asserts. The <b>only</b> source of the
  /// guard→witness map — every field below is read off the gold, none is written here.</summary>
  public sealed record DeclaredRule(
    string GoldId,
    string Section,
    string RuleId,
    string? Impl,
    string? Desc,
    IReadOnlyList<string> Cr,
    bool JudgePassed
  );

  /// <summary>One rule an edge cites (<c>edges[].rule</c>) — the realization half.</summary>
  public sealed record EdgeCitation(string GoldId, string EdgeId, string RuleId, string? Tier);

  /// <summary>A rule the committed rollup carries, with the witnesses the rollup attributes to it.</summary>
  public sealed record RollupRule(string Section, string RuleId, string Status, IReadOnlyList<string> Witnesses);

  /// <summary>A source-code reference to a rule id, found by literal scan of the engine sources.</summary>
  public sealed record CodeReference(string Path, int Line);

  /// <summary>One row of the guard→witness map: a residual rule and everything derived about it.</summary>
  public sealed record WitnessedRule(
    string RuleId,
    string Section,
    string? Impl,
    string Status,
    IReadOnlyList<string> Witnesses,
    IReadOnlyList<string> CitingGolds,
    IReadOnlyList<CodeReference> CodeReferences,
    string? Desc,
    IReadOnlyList<string> Cr
  )
  {
    public bool Unwitnessed => Witnesses.Count == 0;

    /// <summary>No engine source names this rule id, so the rule→code leg of §2's bijection cannot be
    /// checked for it. Reported, not gated — closing this leg is issue #34's job.</summary>
    public bool CodeUnlinked => CodeReferences.Count == 0;
  }

  /// <summary>A rollup rule whose witness attribution disagrees with the golds' own <c>declares</c>.</summary>
  public sealed record WitnessDisagreement(string RuleId, IReadOnlyList<string> InGoldsOnly, IReadOnlyList<string> InRollupOnly);

  /// <summary>The join-2 result — the guard→witness map plus its three consistency deltas.</summary>
  public sealed record GuardWitnessJoin(
    IReadOnlyList<WitnessedRule> Rules,
    IReadOnlyList<WitnessDisagreement> Disagreements,
    IReadOnlyList<string> RollupRulesMissingFromGolds,
    IReadOnlyList<string> GoldRulesMissingFromRollup,
    IReadOnlyList<EdgeCitation> DanglingCitations,
    int GoldsScanned,
    int GoldsDeclaringRules,
    int RollupRuleCount,
    int SourceFilesScanned
  )
  {
    public IReadOnlyList<WitnessedRule> Unwitnessed => [.. Rules.Where(r => r.Unwitnessed)];

    public IReadOnlyList<WitnessedRule> CodeUnlinked => [.. Rules.Where(r => r.CodeUnlinked)];

    /// <summary>No golds, no rules, or no rollup ⇒ the join proves nothing.</summary>
    public bool Vacuous => GoldsScanned == 0 || Rules.Count == 0 || RollupRuleCount == 0;
  }

  /// <summary>
  /// Materializes the guard→witness map. <b>Derivation, in full:</b> group the golds' <c>declares</c>
  /// entries by rule id; the witness set of a rule <i>is</i> the set of gold ids that declared it. The
  /// rollup and the source scan are then joined <i>onto</i> that map to check the two downstream legs —
  /// they never contribute a rule or a witness of their own.
  /// </summary>
  /// <param name="declared">Every <c>declares[]</c> entry across the golds.</param>
  /// <param name="citations">Every <c>edges[].rule</c> citation across the golds.</param>
  /// <param name="rollup">The committed rollup's rules (the <c>.cited</c> twin, for its witnesses).</param>
  /// <param name="codeReferencesByRuleId">Rule id → engine-source occurrences (derived by literal scan).</param>
  /// <param name="goldsScanned">How many golds were read — the non-vacuity denominator.</param>
  /// <param name="sourceFilesScanned">How many engine sources were scanned — the other denominator.</param>
  public static GuardWitnessJoin JoinGuardsToWitnesses(
    IEnumerable<DeclaredRule> declared,
    IEnumerable<EdgeCitation> citations,
    IEnumerable<RollupRule> rollup,
    IReadOnlyDictionary<string, IReadOnlyList<CodeReference>> codeReferencesByRuleId,
    int goldsScanned,
    int sourceFilesScanned
  )
  {
    var declaredList = declared.ToList();
    var citationList = citations.ToList();
    var rollupById = rollup.ToDictionary(r => r.RuleId, StringComparer.Ordinal);

    var citingByRule = citationList
      .GroupBy(c => c.RuleId, StringComparer.Ordinal)
      .ToDictionary(
        g => g.Key,
        g => (IReadOnlyList<string>)[.. g.Select(c => c.GoldId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)],
        StringComparer.Ordinal
      );

    var rules = new List<WitnessedRule>();
    foreach (var group in declaredList.GroupBy(d => d.RuleId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal))
    {
      var witnesses = group.Select(d => d.GoldId).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
      var first = group.First();
      rules.Add(
        new WitnessedRule(
          RuleId: group.Key,
          Section: first.Section,
          Impl: group.Select(d => d.Impl).FirstOrDefault(i => i is not null),
          // observed(1) → corroborated(≥2) → confirmed(a witness judge-PASSed) — the same ladder the
          // rollup computes, recomputed here from the golds so the two can be compared rather than
          // one trusting the other.
          Status: group.Any(d => d.JudgePassed) ? "confirmed" : witnesses.Count >= 2 ? "corroborated" : "observed",
          Witnesses: witnesses,
          CitingGolds: citingByRule.TryGetValue(group.Key, out var c) ? c : [],
          CodeReferences: codeReferencesByRuleId.TryGetValue(group.Key, out var refs) ? refs : [],
          Desc: group.Select(d => d.Desc).FirstOrDefault(d => !string.IsNullOrEmpty(d)),
          Cr: group.SelectMany(d => d.Cr).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList()
        )
      );
    }

    var byId = rules.ToDictionary(r => r.RuleId, StringComparer.Ordinal);

    var disagreements = new List<WitnessDisagreement>();
    foreach (var (rid, rr) in rollupById)
    {
      if (!byId.TryGetValue(rid, out var mine))
        continue;
      var inGolds = mine.Witnesses.Except(rr.Witnesses, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
      var inRollup = rr.Witnesses.Except(mine.Witnesses, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
      if (inGolds.Count > 0 || inRollup.Count > 0)
        disagreements.Add(new WitnessDisagreement(rid, inGolds, inRollup));
    }

    return new GuardWitnessJoin(
      Rules: rules,
      Disagreements: [.. disagreements.OrderBy(d => d.RuleId, StringComparer.Ordinal)],
      RollupRulesMissingFromGolds: [.. rollupById.Keys.Where(k => !byId.ContainsKey(k)).OrderBy(x => x, StringComparer.Ordinal)],
      GoldRulesMissingFromRollup: [.. byId.Keys.Where(k => !rollupById.ContainsKey(k)).OrderBy(x => x, StringComparer.Ordinal)],
      DanglingCitations: [.. citationList.Where(c => !byId.ContainsKey(c.RuleId)).OrderBy(c => c.GoldId, StringComparer.Ordinal).ThenBy(c => c.EdgeId, StringComparer.Ordinal)],
      GoldsScanned: goldsScanned,
      GoldsDeclaringRules: declaredList.Select(d => d.GoldId).Distinct(StringComparer.Ordinal).Count(),
      RollupRuleCount: rollupById.Count,
      SourceFilesScanned: sourceFilesScanned
    );
  }
}
