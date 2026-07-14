using Flowthru.Step;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Shared;

namespace MagicAtlas.Flows.CardAtlas.Steps;

/// <summary>
/// D2 + D3 — the family-level rollups, aggregated from D1 (CardPorts) and D4 (ComboInstances) with NO
/// re-materialize. D2 (<see cref="ResourceGraph"/>): family stations sized by card mass (from D1) + the
/// directed lines the reconstructed combos actually traverse (from D4 rings), each annotated with how many
/// combos realize it, the best tier, and whether it's a bidirectional engine. D3
/// (<see cref="ArchetypeCatalog"/>): the realized combo-shape catalog — every family-signature ≥1 combo
/// realizes, with combo count, best tier, green-fraction, an example piece list, and the produced results.
/// (The full STRUCTURAL catalog of theoretically-possible shapes stays in the PortGraphAtlas report,
/// now untruncated.)
///
/// <para>Promoted from tests/magic-ast-tests/Flows/CardAtlas/Steps/FamilyRollupStep.cs.</para>
/// </summary>
[FlowthruStep]
public static class FamilyRollupStep
{
  private static int TierRank(string tier) =>
    tier switch { "Green" => 0, "Amber" => 1, _ => 2 };

  private static string TierName(int rank) =>
    rank switch { 0 => "Green", 1 => "Amber", 2 => "Red", _ => "" };

  public static Func<
    (IEnumerable<CardPortRow> Ports, IEnumerable<ComboInstanceRow> Combos),
    (ResourceGraph, ArchetypeCatalog)
  > Create() =>
    inputs =>
    {
      var ports = inputs.Ports.ToList();
      var combos = inputs.Combos.ToList();

      // ── D2 stations: canonical families, sized by distinct card mass (from D1). ──
      var stations = ports
        .Where(p => ResourceFamilies.Canonical.Contains(p.Family))
        .GroupBy(p => p.Family, StringComparer.Ordinal)
        .Select(g => new ResourceStation
        {
          Family = g.Key,
          Cards = g.Select(p => p.Card).Distinct(StringComparer.Ordinal).Count(),
          Labels = g.Select(p => p.Label).Distinct(StringComparer.Ordinal).Count(),
        })
        .OrderByDescending(s => s.Cards)
        .ThenBy(s => s.Family, StringComparer.Ordinal)
        .ToList();

      // ── D2 lines: the family→family hops the reconstructed combos traverse (from D4 rings). ──
      var lineCombos = new Dictionary<(string From, string To), HashSet<string>>();
      var lineBestTier = new Dictionary<(string From, string To), int>();
      foreach (var c in combos)
      {
        var ring = c.FamilyRing.Split(" → ", StringSplitOptions.RemoveEmptyEntries);
        if (ring.Length < 2)
          continue; // single-family engine — no inter-station transition
        for (var i = 0; i < ring.Length; i++)
        {
          var from = ring[i];
          var to = ring[(i + 1) % ring.Length]; // wraparound closes the loop
          if (string.Equals(from, to, StringComparison.Ordinal))
            continue;
          var key = (from, to);
          if (!lineCombos.TryGetValue(key, out var set))
            lineCombos[key] = set = new HashSet<string>(StringComparer.Ordinal);
          set.Add(c.ComboId);
          var rank = TierRank(c.Tier);
          lineBestTier[key] = lineBestTier.TryGetValue(key, out var cur) ? Math.Min(cur, rank) : rank;
        }
      }
      var lines = lineCombos
        .Select(kv => new ResourceLine
        {
          From = kv.Key.From,
          To = kv.Key.To,
          RealizingCombos = kv.Value.Count,
          BestTier = TierName(lineBestTier[kv.Key]),
          Engine = lineCombos.ContainsKey((kv.Key.To, kv.Key.From)),
        })
        .OrderByDescending(l => l.RealizingCombos)
        .ThenBy(l => l.From, StringComparer.Ordinal)
        .ThenBy(l => l.To, StringComparer.Ordinal)
        .ToList();

      var graph = new ResourceGraph
      {
        GeneratedAt = DateTime.UtcNow,
        Stations = stations,
        Lines = lines,
      };

      // ── D3 catalog: realized archetypes (family-signatures with ≥1 reconstructed combo). ──
      var entries = combos
        .GroupBy(c => c.FamilySignature, StringComparer.Ordinal)
        .Select(g =>
        {
          var byPop = g.OrderByDescending(c => c.Popularity)
            .ThenBy(c => c.ComboId, StringComparer.Ordinal)
            .ToList();
          var distinctCombos = g.Select(c => c.ComboId).Distinct(StringComparer.Ordinal).Count();
          var green = g.Count(c => c.Tier == "Green");
          var results = g.SelectMany(c => c.Results.Split("; ", StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();
          return new ArchetypeEntry
          {
            Families = g.Key,
            FamilyCount = g.Key.Split(", ", StringSplitOptions.RemoveEmptyEntries).Length,
            RealizingCombos = distinctCombos,
            BestTier = g.Any(c => c.Tier == "Green") ? "Green" : "Amber",
            GreenFraction = Math.Round(green / (double)g.Count(), 3),
            ExampleCards = byPop[0].Cards,
            Results = string.Join("; ", results),
          };
        })
        .OrderByDescending(e => e.RealizingCombos)
        .ThenBy(e => e.FamilyCount)
        .ThenBy(e => e.Families, StringComparer.Ordinal)
        .ToList();

      var catalog = new ArchetypeCatalog
      {
        GeneratedAt = DateTime.UtcNow,
        RealizedArchetypes = entries.Count,
        Entries = entries,
      };

      Console.Error.WriteLine(
        $"[FamilyRollup] {stations.Count} stations, {lines.Count} realized lines, {entries.Count} realized archetypes"
      );
      return (graph, catalog);
    };
}
