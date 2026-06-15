using MagicAtlas.Bench;

// Combo-recall benchmark CLI (alignment initiative 04, Track A).
//
//   dotnet run                 → run the bench and PRINT the report (does not touch the baseline)
//   dotnet run -- --write      → run the bench and WRITE bench-report.json (the committed baseline)
//
// The NUnit ratchet (ComboRecallRatchetTest) is the gate: it asserts recall never decreases vs the
// committed baseline, and updates the baseline when recall increases.

var write = args.Contains("--write");

var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);
var report = runner.Run(snapshot);

Console.WriteLine(
  $"Commander Spellbook combo-recall bench (CSB {snapshot.CsbVersion} @ {snapshot.CsbTimestamp})"
);
Console.WriteLine($"  combosEligible      : {report.CombosEligible}");
Console.WriteLine($"  reconstructedGreen  : {report.ReconstructedGreen}");
Console.WriteLine($"  reconstructedAmber  : {report.ReconstructedAmber}");
Console.WriteLine($"  missed              : {report.Missed}");
Console.WriteLine($"  recallAtGreen       : {report.RecallAtGreen:0.0000}");
Console.WriteLine($"  recallAtAmber       : {report.RecallAtAmber:0.0000}");

if (write)
{
  BenchReportJson.Write(BenchPaths.BaselineReportPath, report);
  Console.WriteLine($"\nWrote baseline → {BenchPaths.BaselineReportPath}");
}
else
{
  Console.WriteLine();
  Console.WriteLine(BenchReportJson.Serialize(report));
}

return 0;
