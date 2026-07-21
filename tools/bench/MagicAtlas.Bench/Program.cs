using MagicAtlas.Bench;

// Combo-recall benchmark CLI (alignment initiative 04, Track A).
//
//   dotnet run                                          → run the bench and PRINT the report (does not
//                                                          touch the report file)
//   dotnet run -- --write                                → run the bench and WRITE bench-report.json
//                                                          (the derived report artifact)
//   dotnet run -- --explain <comboId>                    → pretty-print WHY a pinned snapshot combo
//                                                          (by id from combo-axis-expectations.json)
//                                                          reconstructs at its tier — the edge trail,
//                                                          per-hop verdicts, cycle-level verdict
//   dotnet run -- --explain-cards "Card A" "Card B" ...  → same, for an AD-HOC card set not in the
//                                                          pinned snapshot
//   dotnet run -- --regenerate-roster                    → recompute combo-axis-expectations.json's
//                                                          `combos` ROSTER (id + cards) from a live run.
//                                                          It rewrites NOTHING else: `axisExceptions` and
//                                                          `unreconstructed` carry a judge-set `verdict`,
//                                                          and a tool that could write those would make
//                                                          the gate assert that the engine agrees with
//                                                          itself (ADR 0004 §5.2). There is deliberately
//                                                          no --regenerate-expectations.
//
// bench-report.json is a DERIVED REPORT, not the gate. The gate is the NUnit ComboAxisExpectationTest:
// every eligible combo is expected to satisfy all five ADR-0002 §8 axes (a certified infinite) unless
// combo-axis-expectations.json carries a judged {combo, axis, verdict} exception for it. A failure names
// WHICH AXIS moved.

var snapshot = ComboSnapshot.Load(BenchPaths.SnapshotPath);
var runner = ComboRecallRunner.Create(BenchPaths.FixturesRoot, BenchPaths.OntologyPath);

var explainIndex = Array.IndexOf(args, "--explain");
if (explainIndex >= 0)
{
  if (explainIndex + 1 >= args.Length)
  {
    Console.Error.WriteLine("Usage: dotnet run -- --explain <comboId>");
    return 1;
  }

  var comboId = args[explainIndex + 1];
  var combo = snapshot.Combos.FirstOrDefault(c => c.Id == comboId);
  if (combo is null)
  {
    Console.Error.WriteLine(
      $"No combo '{comboId}' in the pinned snapshot ({BenchPaths.SnapshotPath}). "
        + "IDs come from combo-axis-expectations.json / the snapshot — check spelling."
    );
    return 1;
  }

  var cardNames = combo.Cards.Select(c => c.Name).Distinct(StringComparer.Ordinal).ToList();
  ComboExplainPrinter.Print(runner.Evaluate(combo, cardNames));
  return 0;
}

var explainCardsIndex = Array.IndexOf(args, "--explain-cards");
if (explainCardsIndex >= 0)
{
  var cardNames = args
    .Skip(explainCardsIndex + 1)
    .TakeWhile(a => !a.StartsWith("--", StringComparison.Ordinal))
    .Distinct(StringComparer.Ordinal)
    .ToList();

  if (cardNames.Count < 2)
  {
    Console.Error.WriteLine("Usage: dotnet run -- --explain-cards \"Card A\" \"Card B\" [...]");
    return 1;
  }

  var adhoc = new SnapshotCombo
  {
    Id = "adhoc",
    Popularity = 0,
    Cards = [.. cardNames.Select(n => new SnapshotCard { Name = n })],
  };
  ComboExplainPrinter.Print(runner.Evaluate(adhoc, cardNames));
  return 0;
}

if (args.Contains("--regenerate-roster"))
{
  ComboRosterRegeneration.Regenerate(BenchPaths.ExpectedTiersPath, runner, snapshot);
  Console.WriteLine(
    $"Regenerated the eligible-set roster (id + cards only; verdicts untouched) → {BenchPaths.ExpectedTiersPath}"
  );
  return 0;
}

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

if (args.Contains("--write"))
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
