namespace MagicAtlas.Bench.Tests;

using System.Text.Json;
using MagicAST.Interaction;

/// <summary>
/// Item R1 — the fidelityRisk GATE: a combo that reconstructs a cycle must not rest on a card whose gold
/// fixture is on <c>tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json</c> (known-drifted oracle
/// text). <c>GoldCorpus</c> reads each fixture's committed <c>Output.Oracle.Abilities</c> directly — never
/// re-parses <c>Input.OracleText</c> against the live corpus — so a fixture whose <c>Output</c> was
/// derived from stale/wrong <c>Input</c> text silently props up whatever its combo(s) certify at, for
/// ANY combo the card feeds. This is the exact check that would have caught this session's Suture Priest
/// incident (a certified-infinite combo resting on a fixture already flagged as debt) automatically,
/// without a judge needing to go digging.
///
/// <para>
/// Scoped to combos that RECONSTRUCT (the old Green/Amber scope, restated after ADR 0004 §5 retired the
/// stored tier): a quarantined card feeding a combo with no reconstruction isn't a false-positive risk —
/// nothing certified reconstruction on top of it, so there is nothing to distrust.
/// </para>
///
/// <para>
/// <b>The acknowledged carve-out.</b> The moment this gate went live it found 15 of the 33 pinned combos
/// (well beyond the single Suture Priest incident) ALREADY resting on tracked quarantine debt — pins that
/// pre-date the gate and were never re-reviewed against it. Rather than either (a) retroactively failing
/// the whole suite on debt nobody consciously accepted, or (b) silently weakening the gate, this mirrors
/// the SAME shrink-only-whitelist pattern <c>oracle-text-quarantine.json</c> itself uses:
/// <c>fidelity-risk-acknowledged.json</c> is an explicit, named, per-(combo, fixture) carve-out — matching
/// is on the PAIR, not the combo id alone, so a NEW/different quarantine hit on an already-acknowledged
/// combo (e.g. a second card in the same combo getting quarantined later) is never silently swallowed by
/// the existing entry; it fails as its own, separate, unacknowledged risk. Every currently-observed risk
/// fixture for a combo must be individually listed in that combo's <c>fixtures</c> array (the loud check);
/// an acknowledged fixture no longer at risk (de-quarantined/fixed) fails until removed — the carve-out
/// only shrinks, per fixture, same as the quarantine it mirrors. Per the governing principle, growing this
/// list is something a sweep may PROPOSE (which is exactly how the seed was produced — see that file's
/// <c>_doc</c>), but accepting a proposed entry is a human-confirmed decision.
/// </para>
/// </summary>
[TestFixture]
public class FidelityRiskGateTest
{
  // The current bench run, keyed by combo id (independent run from ComboAxisExpectationTest's — cheap, and
  // keeps this gate's failure self-contained/greppable without depending on that fixture's private state).
  private static readonly Lazy<IReadOnlyDictionary<string, ComboResult>> _current = new(RunCurrent);

  // The quarantine index, reloaded directly (not via ComboResult) so the failure message can surface the
  // hand-authored 'reason' field too — QuarantinedCard's schema is deliberately minimal (Card/Fixture/Tag).
  private static readonly Lazy<QuarantineIndex> _quarantine = new(
    () => QuarantineIndex.Load(BenchPaths.QuarantinePath)
  );

  // (comboId, fixturePath) pairs, not comboId alone — so a NEW/different quarantine hit on an
  // already-acknowledged combo (e.g. a second card in the combo getting quarantined later) is never
  // silently swallowed by the combo-level entry; it fails as its own, separate, unacknowledged risk.
  private static readonly Lazy<IReadOnlySet<(string Id, string Fixture)>> _acknowledged = new(LoadAcknowledged);

  [Test]
  public void Pinned_run_is_present()
  {
    Assert.That(_current.Value, Is.Not.Empty, "The bench produced no eligible combos — unexpected.");
  }

  [TestCaseSource(nameof(ReconstructingComboCases))]
  public void Reconstructing_combo_has_no_unacknowledged_fidelity_risk(
    string id,
    IReadOnlyList<string> cards
  )
  {
    Assert.That(
      _current.Value.ContainsKey(id),
      Is.True,
      $"Combo '{id}' is rostered but is no longer eligible in the run — see ComboAxisExpectationTest."
    );

    // The case source is scoped to combos that reconstruct SOMETHING (the old Green/Amber scope): a
    // quarantined card feeding a combo with no reconstruction is not a false-positive risk, because
    // nothing was certified on top of it. So a null here is a genuine, separate regression.
    var diagnostics = _current.Value[id].Diagnostics;
    Assert.That(
      diagnostics,
      Is.Not.Null,
      $"Combo '{id}' reconstructs no cycle but is not listed under 'unreconstructed' — see "
        + "ComboAxisExpectationTest, which owns that drift."
    );

    var certification = ComboPlainLanguage.Describe(ComboAxisVector.FromDiagnostics(diagnostics!));

    var risk = RiskOrEmpty(_current.Value[id].FidelityRisk);
    var currentFixtures = risk.Select(r => r.Fixture).ToHashSet(StringComparer.Ordinal);
    var acknowledgedFixtures = _acknowledged.Value
      .Where(p => p.Id == id)
      .Select(p => p.Fixture)
      .ToHashSet(StringComparer.Ordinal);

    // Every CURRENTLY-observed risk fixture must be individually acknowledged — a fixture at risk that
    // isn't in this combo's acknowledged set is a NEW/unreviewed risk (never silently swallowed just
    // because some OTHER fixture on this same combo was previously accepted).
    var unacknowledged = currentFixtures.Except(acknowledgedFixtures).ToList();
    Assert.That(
      unacknowledged,
      Is.Empty,
      unacknowledged.Count > 0
        ? BuildMessage(id, certification, cards, risk.Where(r => unacknowledged.Contains(r.Fixture)).ToList())
        : ""
    );

    // Shrink-only, per fixture: an acknowledged fixture no longer at risk (de-quarantined/fixed) must be
    // REMOVED from its entry's 'fixtures' array — same ratchet direction as the quarantine itself.
    var stale = acknowledgedFixtures.Except(currentFixtures).ToList();
    Assert.That(
      stale,
      Is.Empty,
      $"Combo '{id}' acknowledges fixture(s) [{string.Join(", ", stale)}] in fidelity-risk-acknowledged.json "
        + "but no longer rests on them (de-quarantined/fixed). Remove them from this entry's 'fixtures' "
        + "array — the carve-out only shrinks, per fixture."
    );
  }

  private static IReadOnlyList<QuarantinedCard> RiskOrEmpty(IReadOnlyList<QuarantinedCard>? risk) => risk ?? [];

  private static string BuildMessage(
    string id,
    string certification,
    IReadOnlyList<string> cards,
    IReadOnlyList<QuarantinedCard>? risk
  )
  {
    var q = _quarantine.Value;
    var detail = string.Join(
      "; ",
      (risk ?? []).Select(r =>
      {
        var reason = q.TryGet(r.Fixture, out var entry) ? entry.Reason : "(reason unavailable)";
        return $"'{r.Card}' -> fixture '{r.Fixture}' (quarantine tag '{r.Tag}': {reason})";
      })
    );

    return $"Combo '{id}' ({string.Join(" + ", cards)}) reconstructs as '{certification}' but rests on QUARANTINED "
      + $"fixture(s): {detail}. GoldCorpus reads each fixture's committed Output.Oracle.Abilities directly "
      + "(never re-parses Input.OracleText), so a fixture already known to drift from its authoritative "
      + "oracle text (tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json) silently props up "
      + $"whatever '{id}' certifies at — the expectation is not trustworthy until the fixture is fixed. Either "
      + "fix the fixture's Input.OracleText (removing it from the quarantine), or, if this is pre-existing, "
      + "already-tracked debt you are knowingly accepting for now, add a named entry to "
      + "fidelity-risk-acknowledged.json (a human-reviewed, shrink-only carve-out — never silently mutate "
      + "it).";
  }

  /// <summary>Every rostered combo that is expected to reconstruct a cycle (i.e. is NOT on the
  /// 'unreconstructed' list) — the successor to the old "pinned Green or Amber" scope.</summary>
  public static IEnumerable<TestCaseData> ReconstructingComboCases()
  {
    var doc = ComboAxisExpectationsJson.Read(BenchPaths.ExpectedTiersPath);
    var unreconstructed = doc.Unreconstructed.Select(u => u.Combo).ToHashSet(StringComparer.Ordinal);
    return doc
      .Combos.Where(c => !unreconstructed.Contains(c.Id))
      .OrderBy(c => c.Id, StringComparer.Ordinal)
      .Select(c => new TestCaseData(c.Id, c.Cards).SetName($"FidelityRisk_{c.Id}"));
  }

  private static IReadOnlyDictionary<string, ComboResult> RunCurrent()
  {
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);
    return runner.Run(snapshot).Combos.ToDictionary(c => c.Id, StringComparer.Ordinal);
  }

  private static IReadOnlySet<(string Id, string Fixture)> LoadAcknowledged()
  {
    var path = BenchPaths.FidelityRiskAcknowledgedPath;
    var set = new HashSet<(string, string)>();
    if (!File.Exists(path))
      return set;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
      foreach (var e in entries.EnumerateArray())
      {
        if (!e.TryGetProperty("id", out var idEl) || idEl.GetString() is not { } id)
          continue;
        if (!e.TryGetProperty("fixtures", out var fixturesEl))
          continue; // malformed entry — treated as acknowledging nothing, not everything
        foreach (var f in fixturesEl.EnumerateArray())
          if (f.GetString() is { } fixture)
            set.Add((id, fixture));
      }

    return set;
  }
}
