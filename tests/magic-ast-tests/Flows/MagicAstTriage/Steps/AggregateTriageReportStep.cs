using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._07_ModelOutput.Schemas;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Clustering;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Reduces per-card parse records into the agent-facing triage report. Combines
/// pattern frequency, conservative-naive coverage gain (Q3 = (b)), per-line
/// Jaccard related-pattern detection (Q1 = (a), threshold 0.2), and clean
/// exemplars ranked by P-purity cleanliness score (Definition D).
/// </summary>
/// <remarks>
/// Reads the ratchet baseline directly via the configured path — passed in as
/// a dependency so the path is owned by the catalog, not hardcoded here.
/// Hand-parsed-coverage detection scans for fixtures matching card names; the
/// fixture directory is also a constructor dep so the search path stays
/// configurable.
/// </remarks>
[FlowthruStep]
public static class AggregateTriageReportStep
{
  private const double JaccardThreshold = 0.2;
  private const int MaxCandidateLinesPerPattern = 10;
  private const int MaxTopGaps = 20;

  /// <summary>
  /// Depth of the data-derived yield-cluster surface
  /// (<see cref="TriageReport.TopYieldClusters"/>) — the PRIMARY pick surface.
  /// Decoupled from the dispatch batch size: this is "how many buildable
  /// families to surface for discovery", set deeper than a single batch so the
  /// long tail (small/partial-card families like specific triggered abilities)
  /// is visible for the orchestrator to weigh, not just the top whole-card
  /// flips. The orchestrator still picks a batch's worth from the top.
  /// </summary>
  private const int YieldClusterSurfaceDepth = 50;

  /// <summary>
  /// Build the triage report.
  /// </summary>
  /// <param name="ratchetBaselinePath">Path to <c>test-baseline.json</c>.</param>
  /// <param name="handParsedFixturesRoot">Directory holding <c>HandParsedCards/</c> fixtures.</param>
  /// <summary>
  /// Composite grouping key for gap aggregation. The triage flow groups
  /// failures by <c>(Pattern, LastAttemptedRule)</c> so the same pattern
  /// produced by two different parser dispatch chains lands as two distinct
  /// gap entries — sharper than grouping by pattern alone.
  /// </summary>
  private readonly record struct GapKey(string Pattern, string? LastAttemptedRule);

  public static Func<IEnumerable<ParseRecord>, TriageReport> Create(
    string ratchetBaselinePath,
    string handParsedFixturesRoot
  ) =>
    records =>
    {
      var all = records.ToList();
      var handParsedNames = LoadHandParsedCardNames(handParsedFixturesRoot);
      var ratchet = ReadRatchetCoverage(ratchetBaselinePath);

      // --- Indices over the corpus ---
      var allLines = all.SelectMany(r => r.Lines.Select(l => (record: r, line: l))).ToList();
      var totalLines = allLines.Count;
      var passingLines = allLines.Count(x => x.line.Patterns.Count == 0);

      var totalCards = all.Count;
      var passingCards = all.Count(r => r.Lines.All(l => l.Patterns.Count == 0));

      // Ability counts are card-level now (one full-card parse per card), so
      // multi-line abilities aren't double-counted across the lines they span.
      var totalAbilities = all.Sum(r => r.TotalAbilities);
      var parsedAbilities = all.Sum(r => r.ParsedAbilities);

      // (pattern, rule) -> line ids (using identity of (cardId, lineIndex))
      var keyToLineIds = new Dictionary<GapKey, HashSet<(string, int)>>();
      // (pattern, rule) -> card ids
      var keyToCardIds = new Dictionary<GapKey, HashSet<string>>();
      // (pattern, rule) -> list of FailurePosition values for mode computation
      var keyToPositions = new Dictionary<GapKey, List<int>>();

      foreach (var (record, line) in allLines)
      {
        // Per-line distinct keys: a single line can contribute at most once
        // per (pattern, rule) to the line/card counts (matches the pre-change
        // .Distinct() semantics on patterns), but every diagnostic instance
        // contributes a FailurePosition sample for the mode.
        var seenKeys = new HashSet<GapKey>();
        foreach (var diag in line.Diagnostics)
        {
          var key = new GapKey(diag.Pattern, diag.LastAttemptedRule);
          if (seenKeys.Add(key))
          {
            if (!keyToLineIds.TryGetValue(key, out var lineSet))
            {
              keyToLineIds[key] = lineSet = new HashSet<(string, int)>();
            }
            lineSet.Add((record.ScryfallId, line.LineIndex));

            if (!keyToCardIds.TryGetValue(key, out var cardSet))
            {
              keyToCardIds[key] = cardSet = new HashSet<string>();
            }
            cardSet.Add(record.ScryfallId);
          }
          if (diag.FailurePosition is int pos)
          {
            if (!keyToPositions.TryGetValue(key, out var positions))
            {
              keyToPositions[key] = positions = new List<int>();
            }
            positions.Add(pos);
          }
        }
      }

      // Distinct-pattern count remains pattern-axis for legacy semantics — it
      // counts unique failure categories, not unique (pattern, rule) pairs.
      var distinctPatterns = keyToLineIds.Keys.Select(k => k.Pattern).Distinct().Count();

      // --- Q3 conservative-naive coverage gain ---
      // For each gap key K: count cards whose ENTIRE key-set across all lines
      // is exactly {K}; resolving K would flip all of these to green. Same
      // idea at the line level for lineCoveragePct.
      var cardKeySets = all.ToDictionary(
        r => r.ScryfallId,
        r => r.Lines
          .SelectMany(l => l.Diagnostics)
          .Select(d => new GapKey(d.Pattern, d.LastAttemptedRule))
          .ToHashSet()
      );

      var keyToExclusiveCards = keyToLineIds.Keys.ToDictionary(
        k => k,
        k => all.Count(r =>
        {
          var ks = cardKeySets[r.ScryfallId];
          return ks.Count == 1 && ks.Contains(k);
        })
      );

      var keyToExclusiveLines = keyToLineIds.Keys.ToDictionary(
        k => k,
        k => allLines.Count(x =>
        {
          var ks = x.line.Diagnostics
            .Select(d => new GapKey(d.Pattern, d.LastAttemptedRule))
            .Distinct()
            .ToList();
          return ks.Count == 1 && ks[0].Equals(k);
        })
      );

      // --- Proximity-weighted (fractional) yield ---
      // Each card touching key K contributes 1 / (distinct gap-keys on that
      // card): a card one gap from completion contributes 1.0, two-away 0.5,
      // three-away 0.33. The sum is K's fractional yield — a continuous
      // generalisation of keyToExclusiveCards (the 1.0 contributors). Computed
      // by a single pass over cards distributing each card's weight to its keys
      // (O(total diagnostics), not O(keys × cards)).
      var keyToFractionalYield = keyToLineIds.Keys.ToDictionary(k => k, _ => 0.0);
      foreach (var r in all)
      {
        var ks = cardKeySets[r.ScryfallId];
        if (ks.Count == 0)
        {
          continue;
        }
        var weight = 1.0 / ks.Count;
        foreach (var k in ks)
        {
          if (keyToFractionalYield.ContainsKey(k))
          {
            keyToFractionalYield[k] += weight;
          }
        }
      }

      // --- Q1 per-line Jaccard related-pattern detection ---
      // Relatedness stays at the *pattern* axis (not the (pattern, rule)
      // composite): the orchestrator's "avoid parallelising related patterns"
      // contract is keyed on the human-facing pattern name, not the parser
      // dispatch chain.
      var patternToLineIds = keyToLineIds
        .GroupBy(kvp => kvp.Key.Pattern)
        .ToDictionary(
          g => g.Key,
          g =>
          {
            var union = new HashSet<(string, int)>();
            foreach (var entry in g)
            {
              union.UnionWith(entry.Value);
            }
            return union;
          }
        );
      var patternList = patternToLineIds.Keys.ToList();
      var patternToRelated = new Dictionary<string, List<string>>(patternList.Count);
      foreach (var a in patternList)
      {
        var related = new List<string>();
        var aLines = patternToLineIds[a];
        foreach (var b in patternList)
        {
          if (a == b)
            continue;
          var bLines = patternToLineIds[b];
          var intersectCount = aLines.Intersect(bLines).Count();
          if (intersectCount == 0)
            continue;
          var unionCount = aLines.Count + bLines.Count - intersectCount;
          var jaccard = (double)intersectCount / unionCount;
          if (jaccard >= JaccardThreshold)
          {
            related.Add(b);
          }
        }
        patternToRelated[a] = related;
      }

      // --- Build top gaps ranked by proximity-weighted fractional yield ---
      // Fractional yield is the primary key (rewards gaps that are the last-or-
      // nearly-last missing piece on many cards); exclusive-card count and raw
      // card count break ties.
      var rankedKeys = keyToLineIds
        .Keys.OrderByDescending(k => keyToFractionalYield[k])
        .ThenByDescending(k => keyToExclusiveCards[k])
        .ThenByDescending(k => keyToCardIds[k].Count)
        .Take(MaxTopGaps)
        .ToList();

      var cardLookup = all.ToDictionary(r => r.ScryfallId);

      var topGaps = rankedKeys
        .Select(
          (key, idx) =>
            BuildGapEntry(
              key,
              idx + 1,
              keyToLineIds[key],
              keyToCardIds[key],
              keyToExclusiveCards[key],
              keyToExclusiveLines[key],
              keyToFractionalYield[key],
              patternToRelated.TryGetValue(key.Pattern, out var rel) ? rel : new List<string>(),
              keyToPositions.TryGetValue(key, out var pos) ? pos : new List<int>(),
              allLines,
              cardLookup,
              handParsedNames,
              totalCards,
              totalLines
            )
        )
        .ToList();

      // Line-frequency-ranked gap surface — same entries, different ranking
      // axis. Surfaces high-frequency parser bail points that have broad
      // corpus impact even when they don't exclusively flip whole cards.
      var topGapsByFreq = keyToLineIds
        .Keys.OrderByDescending(k => keyToLineIds[k].Count)
        .ThenByDescending(k => keyToCardIds[k].Count)
        .Take(MaxTopGaps)
        .Select(
          (key, idx) =>
            BuildGapEntry(
              key,
              idx + 1,
              keyToLineIds[key],
              keyToCardIds[key],
              keyToExclusiveCards[key],
              keyToExclusiveLines[key],
              keyToFractionalYield[key],
              patternToRelated.TryGetValue(key.Pattern, out var rel2) ? rel2 : new List<string>(),
              keyToPositions.TryGetValue(key, out var pos2) ? pos2 : new List<int>(),
              allLines,
              cardLookup,
              handParsedNames,
              totalCards,
              totalLines
            )
        )
        .ToList();

      // Data-derived clustering pass: lexical-template clustering + greedy
      // set-cover yield projection. Independent of the hand-coded pattern
      // taxonomy above. Orchestrator should weigh both surfaces.
      var topYieldClusters = YieldClusterAnalyzer.ComputeTopYieldClusters(
        all,
        YieldClusterSurfaceDepth,
        handParsedNames
      );

      return new TriageReport
      {
        GeneratedAt = DateTime.UtcNow,
        GlobalMetrics = new GlobalMetrics
        {
          CardCoverage = StatOf(passingCards, totalCards),
          LineCoverage = StatOf(passingLines, totalLines),
          AbilityCoverage = StatOf(parsedAbilities, totalAbilities),
          DistinctUnresolvedPatterns = distinctPatterns,
          HandParsedCoverage = ratchet,
        },
        TopYieldClusters = topYieldClusters,
        TopGaps = topGaps,
        TopGapsByLineFrequency = topGapsByFreq,
      };
    };

  private static GapEntry BuildGapEntry(
    GapKey key,
    int rank,
    HashSet<(string, int)> lineIds,
    HashSet<string> cardIds,
    int exclusiveCards,
    int exclusiveLines,
    double fractionalYield,
    IReadOnlyList<string> related,
    IReadOnlyList<int> positions,
    IReadOnlyList<(ParseRecord record, LineOutcome line)> allLines,
    IReadOnlyDictionary<string, ParseRecord> cardLookup,
    IReadOnlySet<string> handParsedNames,
    int totalCards,
    int totalLines
  )
  {
    // Cleanliness is computed against this gap's (pattern, rule) key
    // specifically, not just the pattern — a line carrying two diagnostics
    // with the same pattern but different last-attempted-rules counts as
    // partially-clean for each gap, which is the right blame attribution.
    var candidateLines = allLines
      .Where(x => x.line.Diagnostics.Any(d =>
        d.Pattern == key.Pattern && d.LastAttemptedRule == key.LastAttemptedRule))
      .Select(x =>
      {
        var totalDiagnostics = x.line.Diagnostics.Count;
        var keyDiagnostics = x.line.Diagnostics.Count(d =>
          d.Pattern == key.Pattern && d.LastAttemptedRule == key.LastAttemptedRule);
        var cleanliness = totalDiagnostics == 0 ? 0.0 : 1.0 - ((double)keyDiagnostics / totalDiagnostics);
        return new CandidateLine
        {
          OracleText = x.line.OracleLine,
          SourceCard = new CandidateLineSource
          {
            Name = x.record.CardName,
            ScryfallId = x.record.ScryfallId,
            Input = x.record.Input,
          },
          CleanlinessScore = cleanliness,
          LineLength = x.line.OracleLine.Length,
          AlreadyHandParsed = handParsedNames.Contains(x.record.CardName),
        };
      })
      .OrderBy(c => c.CleanlinessScore)
      .ThenBy(c => c.LineLength)
      .Take(MaxCandidateLinesPerPattern)
      .ToList();

    return new GapEntry
    {
      Rank = rank,
      Pattern = key.Pattern,
      LastAttemptedRule = key.LastAttemptedRule,
      ModeFailurePosition = ModeOrNull(positions),
      Frequency = new GapFrequency { Lines = lineIds.Count, Cards = cardIds.Count },
      ProjectedCoverageGain = new CoverageGain
      {
        CardCoveragePct = totalCards == 0 ? 0.0 : 100.0 * exclusiveCards / totalCards,
        LineCoveragePct = totalLines == 0 ? 0.0 : 100.0 * exclusiveLines / totalLines,
      },
      FractionalYield = fractionalYield,
      RelatedPatterns = related,
      CandidateLines = candidateLines,
    };
  }

  /// <summary>
  /// Returns the most-common value in <paramref name="values"/>, or null when
  /// the list is empty. Ties are broken by the smaller offset — deterministic
  /// and matches the intuition that earlier-in-the-clause failure positions
  /// are more informative for triage.
  /// </summary>
  private static int? ModeOrNull(IReadOnlyList<int> values)
  {
    if (values.Count == 0)
    {
      return null;
    }
    return values
      .GroupBy(v => v)
      .OrderByDescending(g => g.Count())
      .ThenBy(g => g.Key)
      .First()
      .Key;
  }

  private static CoverageStat StatOf(int passing, int total) =>
    new()
    {
      Passing = passing,
      Total = total,
      Pct = total == 0 ? 0.0 : 100.0 * passing / total,
    };

  /// <summary>
  /// Reads the ratchet baseline JSON and projects it into a <see cref="CoverageStat"/>.
  /// Tolerant of a missing file (returns zeros) — the flow can still produce a
  /// valid report before the consolidation lands.
  /// </summary>
  private static CoverageStat ReadRatchetCoverage(string path)
  {
    if (!File.Exists(path))
    {
      return new CoverageStat { Passing = 0, Total = 0, Pct = 0.0 };
    }

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (!doc.RootElement.TryGetProperty("testResults", out var results))
    {
      return new CoverageStat { Passing = 0, Total = 0, Pct = 0.0 };
    }

    int passing = 0;
    int total = 0;
    foreach (var entry in results.EnumerateObject())
    {
      total++;
      if (
        entry.Value.TryGetProperty("passed", out var passed)
        && passed.ValueKind == JsonValueKind.True
      )
      {
        passing++;
      }
    }

    return StatOf(passing, total);
  }

  /// <summary>
  /// Scans the fixtures directory for hand-parsed card JSONs and returns the
  /// set of card names already covered.
  /// </summary>
  private static IReadOnlySet<string> LoadHandParsedCardNames(string fixturesRoot)
  {
    var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(fixturesRoot))
    {
      return names;
    }

    foreach (var path in Directory.EnumerateFiles(fixturesRoot, "*.json", SearchOption.AllDirectories))
    {
      try
      {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (
          doc.RootElement.TryGetProperty("input", out var input)
          && input.TryGetProperty("name", out var name)
          && name.ValueKind == JsonValueKind.String
        )
        {
          names.Add(name.GetString()!);
        }
      }
      catch (JsonException)
      {
        // Malformed fixture — skip; the ratchet will surface the failure.
      }
    }
    return names;
  }
}
