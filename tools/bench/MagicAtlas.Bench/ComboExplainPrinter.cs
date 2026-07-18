namespace MagicAtlas.Bench;

/// <summary>
/// Human-readable console rendering of a <see cref="ComboResult"/> — the edge trail, each hop's
/// tier/reliability/reason/gated status, and the cycle-level verdict + limiting reason. Backs the
/// <c>--explain</c> / <c>--explain-cards</c> CLI flags (Program.cs), retiring the pattern where answering
/// "why is this combo AMBER" meant writing a throwaway diagnostic test.
/// </summary>
public static class ComboExplainPrinter
{
  public static void Print(ComboResult result)
  {
    Console.WriteLine($"Combo {result.Id}  [{string.Join(" + ", result.Cards)}]");
    Console.WriteLine($"  outcome : {result.Outcome}");

    if (result.FidelityRisk is { Count: > 0 } risk)
    {
      Console.WriteLine("  fidelityRisk (QUARANTINED fixture(s) feed this combo):");
      foreach (var r in risk)
        Console.WriteLine($"    {r.Card}  fixture={r.Fixture}  tag={r.Tag}");
    }

    if (result.Outcome == ReconstructionOutcome.Missed || result.Diagnostics is null)
    {
      Console.WriteLine("  no spanning cycle reconstructs this combo over the current gold ASTs (Missed).");
      return;
    }

    var d = result.Diagnostics;
    Console.WriteLine($"  cycleCards      : {string.Join(", ", result.CycleCards)}");
    Console.WriteLine();
    Console.WriteLine("  verdict:");
    Console.WriteLine($"    cycleTier         : {d.CycleTier}");
    Console.WriteLine($"    cycleLength       : {d.CycleLength}");
    Console.WriteLine($"    firable           : {d.Firable}");
    Console.WriteLine($"    tapRenewed        : {d.TapRenewed}");
    Console.WriteLine($"    coCostsSatisfied  : {d.CoCostsSatisfied}");
    Console.WriteLine($"    balanced          : {d.Balanced}");
    Console.WriteLine($"    lifeBalanced      : {d.LifeBalanced}");
    Console.WriteLine($"    productive        : {d.Productive}");
    Console.WriteLine(
      $"    limitingReason    : {(d.LimitingReason.Length > 0 ? d.LimitingReason : "(none — Green)")}"
    );
    Console.WriteLine();
    Console.WriteLine("  edge trail:");
    foreach (var hop in d.Edges)
    {
      Console.WriteLine(
        $"    [{hop.Hop}] {hop.FromCard}::{hop.FromLabel}  ->  {hop.ToCard}::{hop.ToLabel}"
      );
      Console.WriteLine(
        $"        tier={hop.EdgeTier,-6} provenance={hop.Provenance,-12} family={hop.Family,-8} "
          + $"overlap={hop.Overlap,-9} reliability={hop.Reliability,-8} gated={hop.Gated}"
          + (hop.Reason.Length > 0 ? $" reason={hop.Reason}" : "")
      );
    }
  }
}
