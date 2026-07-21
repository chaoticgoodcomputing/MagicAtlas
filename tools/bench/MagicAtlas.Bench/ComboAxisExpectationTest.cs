namespace MagicAtlas.Bench.Tests;

using MagicAST.Interaction;

/// <summary>
/// The combo-recall GATE, re-pinned at the AXIS level (ADR 0004 §5, issue #31).
///
/// <para><b>What replaced what.</b> This gate used to store, per combo, a copy of what the engine
/// produced — a tier string plus a whole <c>expected</c> diagnostics block including the winning cycle's
/// limiting hop — and assert the live run still equalled it. That is a golden-file test with 33 golden
/// files, and it churned on changes with no semantic content: the <c>LimitingHop</c> null-when-nothing-
/// limits fix (<c>776ff939</c>) moved 18 of 33 pins for zero semantic change. What is pinned now is
/// <b>which axes hold</b>, never which hop limited and never a colour.</para>
///
/// <list type="bullet">
///   <item><b>The default is stateless.</b> Every eligible combo is expected to satisfy all five §8 axes
///   — to be a certified infinite. No entry is needed to express that, so a newly-eligible combo is
///   covered the moment it appears and a silently-degrading one has nowhere to hide.</item>
///   <item><b>An exception is <c>{combo, axis, verdict}</c>.</b> The engine computes <i>that</i> an axis
///   fails; a human rules <i>that this is genuine</i>. Only the verdict is hand-set. Nothing regenerates
///   it — a gate whose expectation is regenerated from the engine's own output asserts that the engine
///   agrees with itself and can never fail (ADR 0004 §5.2).</item>
///   <item><b>A failure names which AXIS moved</b> — <c>+Balanced</c> / <c>-LifeBalanced</c> — and prints
///   the exact JSON entry to paste, stamped <c>UNJUDGED</c>, which is itself a hard failure until ruled
///   on.</item>
/// </list>
///
/// <para><b>Honest classification.</b> The exception list is per-combo state: a NARROWER PIN, not a
/// stateless invariant. The roster (<c>combos</c>) is Derived and gate-checked; the exception
/// <c>verdict</c> is Evidence. The <c>note</c> field is narrative only — no gate and no report treats it
/// as truth.</para>
/// </summary>
[TestFixture]
public class ComboAxisExpectationTest
{
  private static readonly Lazy<IReadOnlyDictionary<string, ComboResult>> _current = new(RunCurrent);
  private static readonly Lazy<ComboAxisExpectationsDocument> _doc = new(LoadDoc);

  [Test]
  public void The_expectation_file_and_the_run_are_both_present()
  {
    Assert.Multiple(() =>
    {
      Assert.That(
        _doc.Value.Combos,
        Is.Not.Empty,
        $"{BenchPaths.ExpectedTiersPath} has an empty roster — regenerate it with "
          + "`dotnet run -- --regenerate-roster`."
      );
      Assert.That(_current.Value, Is.Not.Empty, "The bench produced no eligible combos — unexpected.");
      // Non-vacuity: an all-certified expectation set would make every axis assertion below trivially
      // satisfied by an engine that lost its §8 accounting entirely.
      Assert.That(
        _doc.Value.AxisExceptions,
        Is.Not.Empty,
        "No axis exceptions at all — either every combo really is a certified infinite (check the run) "
          + "or the §8 accounting stopped firing. Either way this gate would be proving nothing."
      );
    });
  }

  /// <summary>The roster is exactly the eligible set, with exactly the right cards. Derived data,
  /// gate-checked — regenerate with <c>--regenerate-roster</c>, never hand-edit.</summary>
  [Test]
  public void The_roster_matches_the_eligible_set_exactly()
  {
    var roster = _doc.Value.Combos.ToDictionary(c => c.Id, StringComparer.Ordinal);

    var runButNotRostered = _current.Value.Keys.Where(id => !roster.ContainsKey(id)).Order(StringComparer.Ordinal).ToList();
    var rosteredButNotRun = roster.Keys.Where(id => !_current.Value.ContainsKey(id)).Order(StringComparer.Ordinal).ToList();
    var cardDrift = roster
      .Where(kv => _current.Value.ContainsKey(kv.Key))
      .Where(kv => !kv.Value.Cards.SequenceEqual(_current.Value[kv.Key].Cards.Distinct(StringComparer.Ordinal), StringComparer.Ordinal))
      .Select(kv => kv.Key)
      .Order(StringComparer.Ordinal)
      .ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        runButNotRostered,
        Is.Empty,
        $"Eligible combo(s) ran but are not on the roster: [{string.Join(", ", runButNotRostered)}]. "
          + "Run `dotnet run -- --regenerate-roster`. Note that a newly-rostered combo is expected to be "
          + "a CERTIFIED INFINITE by default — if it is not, the axis test below will tell you exactly "
          + "which exception entries to add."
      );
      Assert.That(
        rosteredButNotRun,
        Is.Empty,
        $"Rostered combo(s) are no longer eligible: [{string.Join(", ", rosteredButNotRun)}]. Run "
          + "`dotnet run -- --regenerate-roster` and delete any now-orphaned axisExceptions entries."
      );
      Assert.That(
        cardDrift,
        Is.Empty,
        $"Rostered combo(s) whose cards[] no longer match the live run: [{string.Join(", ", cardDrift)}]. "
          + "Run `dotnet run -- --regenerate-roster`."
      );
    });
  }

  /// <summary>
  /// THE GATE. For each rostered combo: the set of §8 axes the live run reports as failing must equal
  /// the set of axes pinned as exceptions. The failure names which axis moved, in which direction.
  /// </summary>
  [TestCaseSource(nameof(RosterCases))]
  public void Combo_satisfies_exactly_its_expected_axes(string id, IReadOnlyList<string> cards)
  {
    Assert.That(
      _current.Value.ContainsKey(id),
      Is.True,
      $"Combo '{id}' is rostered but no longer eligible — see The_roster_matches_the_eligible_set_exactly."
    );
    var result = _current.Value[id];

    var expectedFailing = _doc.Value.ExpectedFailingAxesByCombo.TryGetValue(id, out var f) ? f : [];
    var unreconstructed = _doc.Value.Unreconstructed.Any(u => u.Combo == id);

    // ── The no-reconstruction case ────────────────────────────────────────────────────────────────
    if (result.Diagnostics is null)
    {
      Assert.Multiple(() =>
      {
        Assert.That(
          unreconstructed,
          Is.True,
          $"Combo '{id}' ({string.Join(" + ", cards)}) reconstructs NO cycle, but is not listed under "
            + "'unreconstructed'. This is a REGRESSION — it used to reconstruct. Investigate the change "
            + "that lost it; do not paper over it by adding an entry."
        );
        Assert.That(
          expectedFailing,
          Is.Empty,
          $"Combo '{id}' has axisExceptions but reconstructs no cycle at all — an axis expectation over "
            + "a nonexistent cycle is meaningless. Remove those entries."
        );
      });
      return;
    }

    Assert.That(
      unreconstructed,
      Is.False,
      $"Combo '{id}' ({string.Join(" + ", cards)}) is listed under 'unreconstructed' but the live run "
        + "DOES reconstruct a cycle. This is an IMPROVEMENT — move it out of 'unreconstructed' and add "
        + "whatever axis exceptions the run reports (the message on the next run will name them)."
    );

    var vector = ComboAxisVector.FromDiagnostics(result.Diagnostics);
    var actualFailing = vector.FailingAxes;

    var appeared = actualFailing.Except(expectedFailing, StringComparer.Ordinal).ToList();
    var disappeared = expectedFailing.Except(actualFailing, StringComparer.Ordinal).ToList();

    if (appeared.Count == 0 && disappeared.Count == 0)
      return;

    var moved = string.Join(
      ", ",
      appeared.Select(a => "+" + a).Concat(disappeared.Select(a => "-" + a))
    );

    var paste = string.Join(
      "\n",
      appeared.Select(a =>
        $"    {{ \"combo\": \"{id}\", \"axis\": \"{a}\", \"verdict\": \"{Verdicts.Unjudged}\", \"note\": \"\" }},"
      )
    );

    Assert.Fail(
      $"Combo '{id}' ({string.Join(" + ", cards)}) — AXIS MOVED: {moved}\n"
        + $"  expected to fail : [{string.Join(", ", expectedFailing)}]\n"
        + $"  actually fails   : [{string.Join(", ", actualFailing)}]\n"
        + $"  plain language   : {ComboPlainLanguage.Describe(vector)}\n"
        + (disappeared.Count > 0
          ? $"  An axis that used to fail now HOLDS ({string.Join(", ", disappeared)}). That is a coverage "
            + "GAIN — delete those exception entries from combo-axis-expectations.json.\n"
          : "")
        + (appeared.Count > 0
          ? "  An axis that used to hold now FAILS. If the engine is right, paste these into "
            + "'axisExceptions' and REPLACE the UNJUDGED verdict with 'genuine' (Magic says so, cite CR) "
            + "or 'modelling-gap' (the model is coarse) — an UNJUDGED verdict is a hard failure by "
            + $"design, because only a human may rule on acceptance:\n{paste}\n"
          : "")
    );
  }

  /// <summary>Every exception is judged, names a real axis, and names a rostered combo. This is the
  /// assertion that keeps the pin from becoming self-certifying.</summary>
  [Test]
  public void Every_exception_is_well_formed_and_judged()
  {
    var rostered = _doc.Value.Combos.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

    var unknownCombo = _doc.Value.AxisExceptions.Where(e => !rostered.Contains(e.Combo)).ToList();
    var unknownAxis = _doc
      .Value.AxisExceptions.Where(e => !ComboPlainLanguage.Axes.Contains(e.Axis, StringComparer.Ordinal))
      .ToList();
    var unjudged = _doc.Value.AxisExceptions.Where(e => !Verdicts.AxisVerdicts.Contains(e.Verdict)).ToList();
    var duplicates = _doc
      .Value.AxisExceptions.GroupBy(e => (e.Combo, e.Axis))
      .Where(g => g.Count() > 1)
      .Select(g => $"{g.Key.Combo}/{g.Key.Axis}")
      .ToList();
    var unknownUnreconstructed = _doc.Value.Unreconstructed.Where(u => !rostered.Contains(u.Combo)).ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        unknownCombo.Select(e => e.Combo),
        Is.Empty,
        "axisExceptions naming a combo that is not on the roster — a stale pin."
      );
      Assert.That(
        unknownAxis.Select(e => $"{e.Combo}/{e.Axis}"),
        Is.Empty,
        $"axisExceptions naming an axis outside the vocabulary [{string.Join(", ", ComboPlainLanguage.Axes)}]."
      );
      Assert.That(
        unjudged.Select(e => $"{e.Combo}/{e.Axis} (verdict '{e.Verdict}')"),
        Is.Empty,
        $"axisExceptions carrying a verdict outside [{string.Join(", ", Verdicts.AxisVerdicts)}]. "
          + $"'{Verdicts.Unjudged}' is the placeholder the gate emits and is ALWAYS a hard failure: the "
          + "engine may compute that an axis fails, but only a human or the interaction-judge may rule "
          + "that the failure is genuine. Regenerating this field would make the gate assert that the "
          + "engine agrees with itself (ADR 0004 §5.2)."
      );
      Assert.That(duplicates, Is.Empty, "duplicate (combo, axis) exception entries.");
      Assert.That(
        unknownUnreconstructed.Select(u => u.Combo),
        Is.Empty,
        "'unreconstructed' naming a combo that is not on the roster."
      );
      Assert.That(
        _doc.Value.Unreconstructed.Where(u => u.Verdict != Verdicts.NoReconstruction).Select(u => u.Combo),
        Is.Empty,
        $"'unreconstructed' entries must carry verdict '{Verdicts.NoReconstruction}'."
      );
      Assert.That(
        _doc.Value.Axes,
        Is.EqualTo(ComboPlainLanguage.Axes),
        "the file's declared axis vocabulary has drifted from ComboPlainLanguage.Axes."
      );
    });
  }

  public static IEnumerable<TestCaseData> RosterCases() =>
    ComboAxisExpectationsJson
      .Read(BenchPaths.ExpectedTiersPath)
      .Combos.OrderBy(c => c.Id, StringComparer.Ordinal)
      .Select(c => new TestCaseData(c.Id, c.Cards).SetName($"Combo_{c.Id}_axes"));

  private static IReadOnlyDictionary<string, ComboResult> RunCurrent()
  {
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);
    return runner.Run(snapshot).Combos.ToDictionary(c => c.Id, StringComparer.Ordinal);
  }

  private static ComboAxisExpectationsDocument LoadDoc()
  {
    var path = BenchPaths.ExpectedTiersPath;
    Assert.That(File.Exists(path), Is.True, $"Missing combo-axis-expectations at {path}.");
    return ComboAxisExpectationsJson.Read(path);
  }
}
