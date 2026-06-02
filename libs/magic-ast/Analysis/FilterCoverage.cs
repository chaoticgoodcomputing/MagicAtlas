namespace MagicAST.Analysis;

using MagicAST.AST.References;

/// <summary>One ranked provenance bucket: an axis name and how often it was the deciding reason.</summary>
public sealed record AxisTally(string Axis, int Count);

/// <summary>
/// A coverage-pressure rollup over a corpus of <see cref="ObjectFilter"/>s: it turns the per-result
/// <see cref="FilterMatch.Reason"/> / <see cref="SubsumeMatch.Reason"/> provenance into a signal —
/// which axes (and which Phase-3 relational gaps) most often force <c>Unknown</c>, i.e. what to
/// build or parse next to unlock the most edges.
/// </summary>
public sealed record FilterCoverageReport
{
  /// <summary>Distinct filters harvested from the corpus.</summary>
  public required int FilterCount { get; init; }

  /// <summary>Unordered filter-pairs actually evaluated (after any cap).</summary>
  public required int PairCount { get; init; }

  /// <summary>True if the filter set was capped before pairing (logged, never silent).</summary>
  public required bool Capped { get; init; }

  /// <summary><see cref="ObjectFilterRelations.Intersects"/> verdict distribution over the pairs.</summary>
  public required IReadOnlyDictionary<string, int> IntersectVerdicts { get; init; }

  /// <summary>Ranked: which axis forced an <c>Intersects</c> <c>Unknown</c> (the coverage signal).</summary>
  public required IReadOnlyList<AxisTally> IntersectUnknownReasons { get; init; }

  /// <summary><see cref="ObjectFilterRelations.Subsumes"/> verdict distribution over the ordered pairs.</summary>
  public required IReadOnlyDictionary<string, int> SubsumeVerdicts { get; init; }

  /// <summary>Ranked: which axis left a <c>Subsumes</c> verdict <c>No</c>/<c>Unknown</c>.</summary>
  public required IReadOnlyList<AxisTally> SubsumeOpenReasons { get; init; }

  /// <summary>Ranked: how many corpus filters constrain each undecided Phase-3 relational axis —
  /// the direct "implement this axis to unlock N filters" demand signal.</summary>
  public required IReadOnlyList<AxisTally> RelationalAxisFrequency { get; init; }
}

/// <summary>Aggregates the relation operators' provenance over a corpus of filters.</summary>
public static class FilterCoverage
{
  /// <summary>Default cap on filters paired, bounding the O(n²) sweep for a diagnostic run.</summary>
  public const int DefaultMaxFilters = 250;

  public static FilterCoverageReport Analyze(
    IReadOnlyList<ObjectFilter> filters,
    TypeOntology ontology,
    int maxFilters = DefaultMaxFilters
  )
  {
    var capped = filters.Count > maxFilters;
    var sample = capped ? filters.Take(maxFilters).ToList() : filters;

    var intersectVerdicts = new Dictionary<string, int>(StringComparer.Ordinal);
    var intersectUnknown = new Dictionary<string, int>(StringComparer.Ordinal);
    var subsumeVerdicts = new Dictionary<string, int>(StringComparer.Ordinal);
    var subsumeOpen = new Dictionary<string, int>(StringComparer.Ordinal);
    var pairs = 0;

    for (var i = 0; i < sample.Count; i++)
      for (var j = i + 1; j < sample.Count; j++)
      {
        pairs++;
        var m = ObjectFilterRelations.Intersects(sample[i], sample[j], ontology);
        Bump(intersectVerdicts, m.Relation.ToString());
        if (m.Relation == FilterRelation.Unknown && m.Reason is not null)
          Bump(intersectUnknown, m.Reason);

        // Subsumes is directional — evaluate both orders.
        foreach (var (sub, sup) in new[] { (sample[i], sample[j]), (sample[j], sample[i]) })
        {
          var s = ObjectFilterRelations.Subsumes(sub, sup, ontology);
          Bump(subsumeVerdicts, s.Value.ToString());
          if (s.Value != Trilean.Yes && s.Reason is not null)
            Bump(subsumeOpen, s.Reason);
        }
      }

    // Single-filter relational (Phase-3) axis frequency — computed over ALL filters, not the sample.
    var axisFrequency = new Dictionary<string, int>(StringComparer.Ordinal);
    foreach (var f in filters)
      foreach (var axis in RelationalAxes(f))
        Bump(axisFrequency, axis);

    return new FilterCoverageReport
    {
      FilterCount = filters.Count,
      PairCount = pairs,
      Capped = capped,
      IntersectVerdicts = intersectVerdicts,
      IntersectUnknownReasons = Ranked(intersectUnknown),
      SubsumeVerdicts = subsumeVerdicts,
      SubsumeOpenReasons = Ranked(subsumeOpen),
      RelationalAxisFrequency = Ranked(axisFrequency),
    };
  }

  private static IEnumerable<string> RelationalAxes(ObjectFilter f)
  {
    if (f.ExiledWith is not null)
      yield return "ExiledWith";
    if (f.SharesColorWith is not null)
      yield return "SharesColorWith";
    if (f.ChosenCharacteristic is not null)
      yield return "ChosenCharacteristic";
    if (f.History is not null)
      yield return "History";
    if (f.AttachedTo is not null)
      yield return "AttachedTo";
  }

  private static void Bump(Dictionary<string, int> tally, string key) =>
    tally[key] = tally.GetValueOrDefault(key) + 1;

  private static IReadOnlyList<AxisTally> Ranked(Dictionary<string, int> tally) =>
    tally
      .OrderByDescending(kv => kv.Value)
      .ThenBy(kv => kv.Key, StringComparer.Ordinal)
      .Select(kv => new AxisTally(kv.Key, kv.Value))
      .ToList();
}
