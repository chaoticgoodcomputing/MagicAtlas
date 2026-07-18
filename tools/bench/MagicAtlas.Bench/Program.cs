using MagicAtlas.Bench;

// Combo-recall benchmark CLI (alignment initiative 04, Track A).
//
//   dotnet run                                          → run the bench and PRINT the report (does not
//                                                          touch the report file)
//   dotnet run -- --write                                → run the bench and WRITE bench-report.json
//                                                          (the derived report artifact)
//   dotnet run -- --explain <comboId>                    → pretty-print WHY a pinned snapshot combo
//                                                          (by id from combo-expected-tiers.json)
//                                                          reconstructs at its tier — the edge trail,
//                                                          per-hop verdicts, cycle-level verdict
//   dotnet run -- --explain-cards "Card A" "Card B" ...  → same, for an AD-HOC card set not in the
//                                                          pinned snapshot
//   dotnet run -- --regenerate-expected-tiers            → recompute combo-expected-tiers.json's
//                                                          mechanistic `expected` block from a live run
//                                                          (never hand-typed); carries `narrative` over
//                                                          verbatim and refuses if any pin's `expectedTier`
//                                                          doesn't already match the live run
//
// bench-report.json is a DERIVED REPORT, not the gate. The gate is the NUnit ComboExpectedTierTest: it
// asserts each eligible combo's CURRENT reconstruction tier equals the tier explicitly pinned for it on
// combo-expected-tiers.json (a loud, per-combo, stateless whitelist — no moving aggregate baseline) AND
// that its live ComboDiagnostics structurally matches the pinned `expected` block.

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
        + "IDs come from combo-expected-tiers.json / the snapshot — check spelling."
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

if (args.Contains("--regenerate-expected-tiers"))
{
  ExpectedTiersMigration.Regenerate(
    BenchPaths.ExpectedTiersPath,
    runner,
    snapshot,
    DateOnly.FromDateTime(DateTime.Now)
  );
  Console.WriteLine(
    $"Regenerated the mechanistic `expected` block for every pin (narrative carried over verbatim) → {BenchPaths.ExpectedTiersPath}"
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
