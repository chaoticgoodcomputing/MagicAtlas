namespace MagicAST.Tests.Infrastructure;

/// <summary>
/// Compares current test results against a baseline to identify changes.
/// </summary>
public static class BaselineComparer
{
  /// <summary>
  /// Compare current test results against baseline.
  /// </summary>
  public static BaselineComparison Compare(
    TestBaseline baseline,
    Dictionary<string, bool> currentResults
  )
  {
    var stable = new List<string>();
    var stablePasses = new List<string>();
    var stableFailures = new List<string>();
    var progressions = new List<string>();
    var regressions = new List<string>();
    var newTests = new List<string>();

    foreach (var (testName, currentPassed) in currentResults)
    {
      if (!baseline.TestResults.TryGetValue(testName, out var baselineResult))
      {
        // New test - not in baseline
        newTests.Add(testName);
      }
      else if (baselineResult.Passed == currentPassed)
      {
        // Stable - no change
        stable.Add(testName);
        if (currentPassed)
        {
          stablePasses.Add(testName);
        }
        else
        {
          stableFailures.Add(testName);
        }
      }
      else if (!baselineResult.Passed && currentPassed)
      {
        // Progression - fail → pass
        progressions.Add(testName);
      }
      else
      {
        // Regression - pass → fail
        regressions.Add(testName);
      }
    }

    return new BaselineComparison
    {
      Stable = stable,
      StablePasses = stablePasses,
      StableFailures = stableFailures,
      Progressions = progressions,
      Regressions = regressions,
      NewTests = newTests,
    };
  }
}

/// <summary>
/// Result of comparing current results against baseline.
/// </summary>
public sealed class BaselineComparison
{
  /// <summary>
  /// Tests that have the same pass/fail state as baseline.
  /// </summary>
  public required List<string> Stable { get; init; }

  /// <summary>
  /// Tests that passed in baseline and still pass.
  /// </summary>
  public required List<string> StablePasses { get; init; }

  /// <summary>
  /// Tests that failed in baseline and still fail.
  /// </summary>
  public required List<string> StableFailures { get; init; }

  /// <summary>
  /// Tests that failed in baseline but now pass (fail → pass).
  /// </summary>
  public required List<string> Progressions { get; init; }

  /// <summary>
  /// Tests that passed in baseline but now fail (pass → fail).
  /// </summary>
  public required List<string> Regressions { get; init; }

  /// <summary>
  /// Tests not present in baseline (new tests).
  /// </summary>
  public required List<string> NewTests { get; init; }

  /// <summary>
  /// Whether there are any regressions.
  /// </summary>
  public bool HasRegressions => Regressions.Count > 0;
}
