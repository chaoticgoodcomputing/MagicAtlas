using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.Common;

namespace MagicAtlas.Ast.Tests.Flows.FreeTextResidualCensus.Steps;

/// <summary>
/// Walks every committed gold through <see cref="FreeTextSinkScanner"/> (shared with the CORE-ring
/// whitelist gate, so the report and the gate cannot disagree about what a sink is) and shapes the
/// initiative-05 burn-down numbers.
/// </summary>
[FlowthruStep]
public static class CensusStep
{
  public static Func<Data._08_Reporting.Schemas.FreeTextResidualCensus> Create(string repoRoot) =>
    () =>
    {
      var scans = FreeTextSinkScanner.ScanAll(repoRoot);
      var whitelist = FreeTextSinkScanner.LoadWhitelist(repoRoot);

      var live = scans
        .SelectMany(s => s.Instances.Where(kv => kv.Value > 0).Select(kv => (s.Card, Sink: kv.Key, kv.Value)))
        .ToList();

      var bySink = FreeTextSinkScanner
        .AllSinks.Select(sink =>
        {
          var rows = live.Where(l => l.Sink == sink).ToList();
          return new FreeTextSinkBurndown
          {
            Sink = sink,
            Instances = rows.Sum(r => r.Value),
            Cards = rows.Count,
            DebtCards = rows.Count(r => whitelist.GetValueOrDefault((r.Card, sink)) == "debt"),
            IrreducibleCards = rows.Count(r =>
              whitelist.GetValueOrDefault((r.Card, sink)) == "irreducible"
            ),
            CardList = rows.Select(r => r.Card).OrderBy(c => c, StringComparer.Ordinal).ToList(),
          };
        })
        .OrderByDescending(b => b.Instances)
        .ThenBy(b => b.Sink, StringComparer.Ordinal)
        .ToList();

      var liveKeys = live.Select(l => (l.Card, l.Sink)).ToHashSet();

      return new Data._08_Reporting.Schemas.FreeTextResidualCensus
      {
        GeneratedAt = DateTime.UtcNow,
        GoldsScanned = scans.Count,
        DistinctAffectedGolds = live.Select(l => l.Card).Distinct(StringComparer.Ordinal).Count(),
        TotalInstances = live.Sum(l => l.Value),
        UnwhitelistedInstances = live
          .Where(l => !whitelist.ContainsKey((l.Card, l.Sink)))
          .Sum(l => l.Value),
        DeadWhitelistEntries = whitelist
          .Keys.Where(k => !liveKeys.Contains(k))
          .Select(k => $"{k.Card} :: {k.Sink}")
          .OrderBy(s => s, StringComparer.Ordinal)
          .ToList(),
        BySink = bySink,
      };
    };
}
