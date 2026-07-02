using MagicAtlas.Bench;

// Combo-recall benchmark CLI (alignment initiative 04, Track A).
//
//   dotnet run                 → run the bench and PRINT the report (does not touch the report file)
//   dotnet run -- --write      → run the bench and WRITE bench-report.json (the derived report artifact)
//
// bench-report.json is a DERIVED REPORT, not the gate. The gate is the NUnit ComboExpectedTierTest: it
// asserts each eligible combo's CURRENT reconstruction tier equals the tier explicitly pinned for it on
// combo-expected-tiers.json (a loud, per-combo, stateless whitelist — no moving aggregate baseline).

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
