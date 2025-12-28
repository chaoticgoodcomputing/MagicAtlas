namespace MagicAST.Tests.Infrastructure;

using System.Collections.Concurrent;

/// <summary>
/// Tracks test failures and categorizes them for summary reporting.
/// This helps identify hotspots in the parser migration work.
/// </summary>
public static class FailureTracker
{
  private static readonly ConcurrentDictionary<string, List<string>> _failuresByPattern = new();
  private static readonly object _lock = new();

  /// <summary>
  /// Records a test failure with its pattern category.
  /// </summary>
  public static void RecordFailure(string testName, string pattern)
  {
    _failuresByPattern.AddOrUpdate(
      pattern,
      _ => new List<string> { testName },
      (_, list) =>
      {
        lock (_lock)
        {
          list.Add(testName);
          return list;
        }
      }
    );
  }

  /// <summary>
  /// Gets a summary of failures grouped by pattern.
  /// </summary>
  public static Dictionary<string, List<string>> GetFailuresByPattern()
  {
    return _failuresByPattern.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
  }

  /// <summary>
  /// Formats a summary report of failures.
  /// </summary>
  public static string GetSummaryReport()
  {
    if (_failuresByPattern.IsEmpty)
    {
      return "✅ No failures recorded.";
    }

    var report = new System.Text.StringBuilder();
    report.AppendLine("\n╔═══════════════════════════════════════════════════════════════");
    report.AppendLine("║ FAILURE HOTSPOTS");
    report.AppendLine("╚═══════════════════════════════════════════════════════════════\n");

    var sortedPatterns = _failuresByPattern
      .OrderByDescending(kvp => kvp.Value.Count)
      .ThenBy(kvp => kvp.Key);

    foreach (var (pattern, tests) in sortedPatterns)
    {
      report.AppendLine($"📍 {pattern} ({tests.Count} failures)");

      // Show first 3 examples
      var examples = tests.Take(3);
      foreach (var test in examples)
      {
        report.AppendLine($"   • {test}");
      }

      if (tests.Count > 3)
      {
        report.AppendLine($"   ... and {tests.Count - 3} more");
      }

      report.AppendLine();
    }

    var totalFailures = _failuresByPattern.Values.Sum(list => list.Count);
    var uniquePatterns = _failuresByPattern.Keys.Count;

    report.AppendLine($"Total: {totalFailures} failures across {uniquePatterns} patterns");

    return report.ToString();
  }

  /// <summary>
  /// Clears all recorded failures.
  /// </summary>
  public static void Clear()
  {
    _failuresByPattern.Clear();
  }
}
