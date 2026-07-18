namespace MagicAtlas.Bench.Tests;

using System.Text.Json;

/// <summary>
/// Item R1 — the fidelityRisk GATE: a pinned Green or Amber combo must not rest on a card whose gold
/// fixture is on <c>tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json</c> (known-drifted oracle
/// text). <c>GoldCorpus</c> reads each fixture's committed <c>Output.Oracle.Abilities</c> directly — never
/// re-parses <c>Input.OracleText</c> against the live corpus — so a fixture whose <c>Output</c> was
/// derived from stale/wrong <c>Input</c> text silently props up whatever tier its combo(s) certify at, for
/// ANY combo the card feeds. This is the exact check that would have caught this session's Suture Priest
/// incident (a Green tier resting on a fixture already flagged as debt) automatically, without a judge
/// needing to go digging.
///
/// <para>
/// Scoped to Green/Amber only (not Missed): a quarantined card feeding a Missed combo isn't a false-
/// positive risk — nothing certified reconstruction on top of it, so there is nothing to distrust.
/// </para>
///
/// <para>
/// <b>The acknowledged carve-out.</b> The moment this gate went live it found 15 of the 33 pinned combos
/// (well beyond the single Suture Priest incident) ALREADY resting on tracked quarantine debt — pins that
/// pre-date the gate and were never re-reviewed against it. Rather than either (a) retroactively failing
/// the whole suite on debt nobody consciously accepted, or (b) silently weakening the gate, this mirrors
/// the SAME shrink-only-whitelist pattern <c>oracle-text-quarantine.json</c> itself uses:
/// <c>fidelity-risk-acknowledged.json</c> is an explicit, named, per-combo carve-out. A combo NOT on it
/// must have zero fidelity risk (the loud check). A combo ON it is allowed to currently have risk, but
/// MUST still have some — an acknowledged entry whose risk evaporates (its card was de-quarantined) fails
/// until removed, so the carve-out list only shrinks, same as the quarantine it mirrors. Per the governing
/// principle, growing this list is something a sweep may PROPOSE (which is exactly how the seed was
/// produced — see that file's <c>_doc</c>), but accepting a proposed entry is a human-confirmed decision;
/// the seed is pending that review.
/// </para>
/// </summary>
[TestFixture]
public class FidelityRiskGateTest
{
  // The current bench run, keyed by combo id (independent run from ComboExpectedTierTest's — cheap, and
  // keeps this gate's failure self-contained/greppable without depending on that fixture's private state).
  private static readonly Lazy<IReadOnlyDictionary<string, ComboResult>> _current = new(RunCurrent);

  // The quarantine index, reloaded directly (not via ComboResult) so the failure message can surface the
  // hand-authored 'reason' field too — QuarantinedCard's schema is deliberately minimal (Card/Fixture/Tag).
  private static readonly Lazy<QuarantineIndex> _quarantine = new(
    () => QuarantineIndex.Load(BenchPaths.QuarantinePath)
  );

  private static readonly Lazy<IReadOnlySet<string>> _acknowledged = new(LoadAcknowledged);

  [Test]
  public void Pinned_run_is_present()
  {
    Assert.That(_current.Value, Is.Not.Empty, "The bench produced no eligible combos — unexpected.");
  }

  [TestCaseSource(nameof(PinnedGreenOrAmberCases))]
  public void Pinned_green_or_amber_combo_has_no_unacknowledged_fidelity_risk(
    string id,
    string tier,
    IReadOnlyList<string> cards
  )
  {
    Assert.That(
      _current.Value.ContainsKey(id),
      Is.True,
      $"Combo '{id}' is pinned '{tier}' but is no longer eligible in the run — see ComboExpectedTierTest."
    );

    var risk = _current.Value[id].FidelityRisk;
    var hasRisk = risk is { Count: > 0 };
    var acknowledged = _acknowledged.Value.Contains(id);

    if (acknowledged)
    {
      // Shrink-only: an acknowledged carve-out whose risk evaporated (the card was de-quarantined) must
      // be REMOVED from fidelity-risk-acknowledged.json — same ratchet direction as the quarantine itself.
      Assert.That(
        hasRisk,
        Is.True,
        $"Combo '{id}' is on fidelity-risk-acknowledged.json but no longer rests on a quarantined fixture "
          + "(its card(s) were de-quarantined). Remove its entry — the carve-out only shrinks."
      );
      return;
    }

    Assert.That(hasRisk, Is.False, BuildMessage(id, tier, cards, risk));
  }

  private static string BuildMessage(
    string id,
    string tier,
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

    return $"Combo '{id}' ({string.Join(" + ", cards)}) is pinned '{tier}' but rests on QUARANTINED "
      + $"fixture(s): {detail}. GoldCorpus reads each fixture's committed Output.Oracle.Abilities directly "
      + "(never re-parses Input.OracleText), so a fixture already known to drift from its authoritative "
      + "oracle text (tests/magic-ast-tests/Fixtures/oracle-text-quarantine.json) silently props up "
      + $"whatever tier '{id}' certifies at — the pin is not trustworthy until the fixture is fixed. Either "
      + "fix the fixture's Input.OracleText (removing it from the quarantine), or, if this is pre-existing, "
      + "already-tracked debt you are knowingly accepting for now, add a named entry to "
      + "fidelity-risk-acknowledged.json (a human-reviewed, shrink-only carve-out — never silently mutate "
      + "it).";
  }

  public static IEnumerable<TestCaseData> PinnedGreenOrAmberCases() =>
    ExpectedTiersJson
      .Read(BenchPaths.ExpectedTiersPath)
      .Combos.Where(c => c.ExpectedTier is "Green" or "Amber")
      .OrderBy(c => c.Id, StringComparer.Ordinal)
      .Select(c => new TestCaseData(c.Id, c.ExpectedTier, c.Cards).SetName($"FidelityRisk_{c.Id}"));

  private static IReadOnlyDictionary<string, ComboResult> RunCurrent()
  {
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);
    return runner.Run(snapshot).Combos.ToDictionary(c => c.Id, StringComparer.Ordinal);
  }

  private static IReadOnlySet<string> LoadAcknowledged()
  {
    var path = BenchPaths.FidelityRiskAcknowledgedPath;
    var set = new HashSet<string>(StringComparer.Ordinal);
    if (!File.Exists(path))
      return set;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
      foreach (var e in entries.EnumerateArray())
        if (e.TryGetProperty("id", out var idEl) && idEl.GetString() is { } id)
          set.Add(id);

    return set;
  }
}
