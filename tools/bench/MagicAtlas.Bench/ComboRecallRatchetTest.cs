namespace MagicAtlas.Bench.Tests;

/// <summary>
/// The combo-recall RATCHET (alignment initiative 04 §3). Re-runs the combo-recall bench over the
/// pinned Spellbook snapshot + the gold corpus and compares it to the committed baseline
/// (<c>bench-report.json</c>):
/// <list type="bullet">
///   <item>recall@Green and recall@(Green+Amber) may NOT decrease vs the baseline — a merged batch
///         that silently loses interaction-reconstruction coverage fails here;</item>
///   <item>an INCREASE rewrites the baseline (so coverage gains stick), and the test still passes;</item>
///   <item>equal recall passes but still rewrites the baseline if the per-combo detail drifted (e.g.
///         a different card now carries the loop) — keeping the committed artifact in sync.</item>
/// </list>
/// This is a ratchet, not a target: the absolute numbers are deliberately low (the engine's §8 floors
/// are conservative). The point is the DIRECTION — coverage must monotonically improve.
/// </summary>
[TestFixture]
public class ComboRecallRatchetTest
{
  private static (BenchReport Baseline, BenchReport Current) Load()
  {
    Assert.That(
      File.Exists(BenchPaths.BaselineReportPath),
      Is.True,
      $"committed baseline missing at {BenchPaths.BaselineReportPath} — run `dotnet run -- --write` to seed it"
    );

    var baseline = BenchReportJson.Read(BenchPaths.BaselineReportPath);
    var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
    var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);
    return (baseline, runner.Run(snapshot));
  }

  [Test]
  public void Green_recall_does_not_decrease_vs_baseline()
  {
    var (baseline, current) = Load();
    Assert.That(
      current.RecallAtGreen,
      Is.GreaterThanOrEqualTo(baseline.RecallAtGreen),
      $"recall@Green regressed: baseline {baseline.RecallAtGreen:0.0000} "
        + $"({baseline.ReconstructedGreen}/{baseline.CombosEligible}) → "
        + $"current {current.RecallAtGreen:0.0000} ({current.ReconstructedGreen}/{current.CombosEligible})"
    );
  }

  [Test]
  public void Green_plus_amber_recall_does_not_decrease_vs_baseline()
  {
    var (baseline, current) = Load();
    Assert.That(
      current.RecallAtAmber,
      Is.GreaterThanOrEqualTo(baseline.RecallAtAmber),
      $"recall@(Green+Amber) regressed: baseline {baseline.RecallAtAmber:0.0000} "
        + $"({baseline.ReconstructedGreen + baseline.ReconstructedAmber}/{baseline.CombosEligible}) → "
        + $"current {current.RecallAtAmber:0.0000} "
        + $"({current.ReconstructedGreen + current.ReconstructedAmber}/{current.CombosEligible})"
    );
  }

  [Test]
  public void Baseline_is_rewritten_when_recall_increases_or_detail_drifts()
  {
    var (baseline, current) = Load();

    var increased =
      current.RecallAtGreen > baseline.RecallAtGreen
      || current.RecallAtAmber > baseline.RecallAtAmber;

    // Neither metric may decrease (the other two tests assert this; assert here too so this test is
    // self-contained and never rewrites a regressed baseline).
    Assert.Multiple(() =>
    {
      Assert.That(current.RecallAtGreen, Is.GreaterThanOrEqualTo(baseline.RecallAtGreen));
      Assert.That(current.RecallAtAmber, Is.GreaterThanOrEqualTo(baseline.RecallAtAmber));
    });

    var currentJson = BenchReportJson.Serialize(current);
    var baselineJson = File.ReadAllText(BenchPaths.BaselineReportPath);
    if (increased || !string.Equals(currentJson, baselineJson, StringComparison.Ordinal))
    {
      File.WriteAllText(BenchPaths.BaselineReportPath, currentJson);
      TestContext.Out.WriteLine(
        increased
          ? $"recall improved → baseline updated (Green {baseline.ReconstructedGreen}→{current.ReconstructedGreen}, "
            + $"Green+Amber {baseline.ReconstructedGreen + baseline.ReconstructedAmber}→{current.ReconstructedGreen + current.ReconstructedAmber})"
          : "per-combo detail drifted at equal recall → baseline re-synced"
      );
    }
  }
}
