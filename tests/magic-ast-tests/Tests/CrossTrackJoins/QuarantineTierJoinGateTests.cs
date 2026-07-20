namespace MagicAST.Tests.Tests.CrossTrackJoins;

using MagicAtlas.Ast.Tests.Flows.CrossTrackJoins;

/// <summary>
/// The <b>ADR-0004 §4 join-1 GATE</b>: quarantined Parse-track oracle text must not underwrite an
/// Interaction-track GREEN.
/// </summary>
/// <remarks>
/// <para><b>Why a join and not a regeneration check.</b> Suture Priest is the sharpest failure in the
/// mast-loop retro precisely <i>because the artifact was correct</i>. <c>oracle-text-quarantine.json</c>
/// was accurate, current, and doing its job; it was simply a Parse-track artifact with no edge to
/// Interaction-track tiering, so the card sat on the quarantine while underwriting a shipped GREEN.
/// Regenerating the quarantine, or gitignoring it, would both have reproduced the same correct file —
/// they verify a fact against itself. Only crossing it with the <i>other</i> track's claims catches it.</para>
///
/// <para><b>Stateless by construction.</b> This gate does not read
/// <c>_08_Reporting/quarantine-tier-join.json</c> — that is a gitignored Flowthru output absent on a
/// clean checkout. It re-runs the same pure <see cref="CrossTrackJoiner"/> over the live committed
/// artifacts of both tracks, exactly as the artifact-census gate re-runs its classifier. No baseline, no
/// count, nothing that ratchets: the invariant is a membership property.</para>
///
/// <para><b>Acknowledgment reuse, not duplication.</b> Named <c>(comboId, fixture)</c> carve-outs come
/// from the existing <c>tools/bench/MagicAtlas.Bench/fidelity-risk-acknowledged.json</c> (item R1). That
/// file's liveness and shrink-only discipline stay with its owning bench gate; this gate only consumes
/// it. The two are complementary, not redundant: R1's gate joins the quarantine to the engine's LIVE
/// run (bench ring, corpus required); this one joins it to the SHIPPED PINS, hermetically, in the CORE
/// ring — so a pin can never be tiered GREEN on drifted text even when nobody runs the bench.</para>
/// </remarks>
[TestFixture]
public class QuarantineTierJoinGateTests
{
  private static readonly string Root = CrossTrackSources.RepoRoot();

  private static readonly Lazy<CrossTrackJoiner.QuarantineTierJoin> Live = new(() =>
    CrossTrackJoiner.JoinQuarantineToTiers(
      CrossTrackSources.LoadQuarantine(Root),
      CrossTrackSources.LoadCardByFixture(Root),
      CrossTrackSources.LoadPins(Root),
      CrossTrackSources.LoadInteractionGoldsByCard(Root),
      CrossTrackSources.LoadAcknowledged(Root)
    )
  );

  // ── THE ACCEPTANCE TEST ─────────────────────────────────────────────────────────────────────────

  /// <summary>
  /// <b>THE acceptance test for this gate.</b> The historical Suture Priest incident, reconstructed
  /// hermetically from the state at <c>295f3506^</c> — the quarantine entry and the combo pin exactly as
  /// they stood, byte-for-byte, the moment before the fix — and fed to the same joiner the gate above
  /// runs. The join must go RED.
  ///
  /// <para>Reconstructed rather than re-broken: the real fixture is never touched, so this proves the
  /// gate on the incident without reintroducing it.</para>
  /// </summary>
  [Test]
  public void Reconstructed_suture_priest_shape_fails_the_join()
  {
    var result = SuturePriestShape();

    Assert.Multiple(() =>
    {
      Assert.That(result.Vacuous, Is.False, "the reconstruction must exercise both sides of the join");
      Assert.That(result.Violations, Has.Count.EqualTo(1), "the Suture Priest crossing must be a violation");
    });

    var violation = result.Violations[0];
    Assert.Multiple(() =>
    {
      Assert.That(violation.ComboId, Is.EqualTo("261-2577-5670"));
      Assert.That(violation.Tier, Is.EqualTo("Green"));
      Assert.That(violation.Card, Is.EqualTo("Suture Priest"));
      Assert.That(violation.Fixture, Is.EqualTo("NPH/SuturePriest"));
      Assert.That(violation.Tag, Is.EqualTo("debt"));
      Assert.That(violation.Acknowledged, Is.False);
    });

    TestContext.WriteLine("RED (reconstructed Suture-Priest shape): " + violation.Explain());
  }

  /// <summary>
  /// The control for the acceptance test: the SAME reconstruction with the quarantine entry removed must
  /// go green. Without this, the test above could be passing because the joiner flags everything.
  /// </summary>
  [Test]
  public void Reconstructed_shape_is_green_once_the_text_is_de_quarantined()
  {
    var result = CrossTrackJoiner.JoinQuarantineToTiers(
      [],
      SuturePriestCardByFixture,
      SuturePriestPins,
      SuturePriestGoldsByCard,
      new HashSet<(string, string)>()
    );

    Assert.That(result.Risks, Is.Empty, "no quarantined text ⇒ no crossing at all");
  }

  /// <summary>
  /// The second control: the same quarantined text under an AMBER pin is not a violation. The gate is
  /// about certification resting on drift, not about drift as such — an AMBER pin already claims less
  /// than the drift could invalidate.
  /// </summary>
  [Test]
  public void Reconstructed_shape_under_an_amber_pin_is_a_risk_but_not_a_violation()
  {
    var result = CrossTrackJoiner.JoinQuarantineToTiers(
      SuturePriestQuarantine,
      SuturePriestCardByFixture,
      [new CrossTrackJoiner.ComboPin("261-2577-5670", "Amber", ["Warren Soultrader", "Gravecrawler", "Suture Priest"])],
      SuturePriestGoldsByCard,
      new HashSet<(string, string)>()
    );

    Assert.Multiple(() =>
    {
      Assert.That(result.Risks, Has.Count.EqualTo(1), "the crossing is still materialized");
      Assert.That(result.Violations, Is.Empty, "but an AMBER pin is not a false certification");
    });
  }

  // ── the live gate ───────────────────────────────────────────────────────────────────────────────

  /// <summary>THE GATE. No shipped GREEN rests on quarantined oracle text without a named carve-out.</summary>
  [Test]
  public void No_green_pin_rests_on_quarantined_oracle_text()
  {
    var violations = Live.Value.Violations;

    Assert.That(
      violations,
      Is.Empty,
      "ADR-0004 §4: quarantined Parse-track oracle text is underwriting an Interaction-track GREEN.\n\n"
        + string.Join("\n", violations.Select(v => "  " + v.Explain()))
        + "\n\nThis is the Suture Priest shape: the quarantine entry is CORRECT — the gold's Input.OracleText "
        + "genuinely drifts from its authoritative source — and the pin certifies GREEN on top of it. Fix by "
        + "ONE of:\n"
        + "  (a) re-point the gold's Input.OracleText to the authoritative text (GoldRegenerationUtility),\n"
        + "      re-derive its Output through the real parser, and remove it from the quarantine — then\n"
        + "      re-pin the combo at whatever tier the corrected text actually supports;\n"
        + "  (b) if this is pre-existing, already-tracked debt being knowingly accepted, add the\n"
        + "      (comboId, fixture) PAIR to tools/bench/MagicAtlas.Bench/fidelity-risk-acknowledged.json\n"
        + "      with a re-verified reason. That is a human-reviewed decision, never a silent edit.\n"
    );
  }

  /// <summary>Both sides of the join must be non-empty. A join that passed because the quarantine failed
  /// to load, or because no pin was read, proves nothing — and "the report WAS the bug" is the failure
  /// this ADR was written after.</summary>
  [Test]
  public void Join_is_non_vacuous()
  {
    Assert.Multiple(() =>
    {
      Assert.That(Live.Value.QuarantinedFixtureCount, Is.GreaterThan(0), "side A (the Parse-track quarantine) came back empty");
      Assert.That(Live.Value.PinCount, Is.GreaterThan(0), "side B (the Interaction-track pins) came back empty");
      Assert.That(Live.Value.GreenPinCount, Is.GreaterThan(0), "no GREEN pin exists — the gate could never fire");
      Assert.That(Live.Value.Risks, Is.Not.Empty, "the two sides did not cross at all — the fixture→card join is broken");
      Assert.That(Live.Value.Vacuous, Is.False);
    });
  }

  /// <summary>Every quarantine key must resolve to a real gold. An unresolvable key silently shrinks side
  /// A — the quiet way this join could stop seeing the very card it is meant to catch.</summary>
  [Test]
  public void Every_quarantine_entry_resolves_to_a_gold()
  {
    Assert.That(
      Live.Value.UnresolvedFixtures,
      Is.Empty,
      "oracle-text-quarantine.json names fixture(s) that no gold under Fixtures/HandParsedCards matches:\n"
        + string.Join("\n", Live.Value.UnresolvedFixtures.Select(f => "  " + f))
        + "\nAn unresolvable key is invisible to this join — remove it, or fix the path."
    );
  }

  // ── the reconstructed inputs (state at 295f3506^, verified against git history) ──────────────────

  /// <summary>
  /// The quarantine entry as it stood at <c>295f3506^</c> — copied verbatim from that revision of
  /// <c>oracle-text-quarantine.json</c>, which carried 80 entries including this one.
  /// </summary>
  private static readonly IReadOnlyList<CrossTrackJoiner.QuarantinedFixture> SuturePriestQuarantine =
  [
    new(
      "NPH/SuturePriest",
      "debt",
      "gold oracle text drifts from authoritative Scryfall — re-point to the real card (gold-fidelity cleanup)"
    ),
  ];

  private static readonly IReadOnlyDictionary<string, string> SuturePriestCardByFixture =
    new Dictionary<string, string>(StringComparer.Ordinal) { ["NPH/SuturePriest"] = "Suture Priest" };

  /// <summary>The pin as it stood at <c>295f3506^</c>: <c>261-2577-5670</c> at <b>Green</b>. Commit
  /// 295f3506 ("correct stale Suture Priest oracle text; combo 261-2577-5670 back to Amber") re-pinned it
  /// to Amber once the real text made the life-gain port honestly optional (Gated ⇒ §8 floors to Amber).</summary>
  private static readonly IReadOnlyList<CrossTrackJoiner.ComboPin> SuturePriestPins =
  [
    new("261-2577-5670", "Green", ["Warren Soultrader", "Gravecrawler", "Suture Priest"]),
  ];

  private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SuturePriestGoldsByCard =
    new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
      ["Gravecrawler"] = ["gravecrawler-x-ashnods-altar-x-blood-artist"],
    };

  private static CrossTrackJoiner.QuarantineTierJoin SuturePriestShape() =>
    CrossTrackJoiner.JoinQuarantineToTiers(
      SuturePriestQuarantine,
      SuturePriestCardByFixture,
      SuturePriestPins,
      SuturePriestGoldsByCard,
      // No acknowledgment existed: fidelity-risk-acknowledged.json did not exist until item R1, which
      // landed AFTER this incident and was seeded BY it.
      new HashSet<(string, string)>()
    );
}
