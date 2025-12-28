namespace MagicAST.Tests.Infrastructure;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Represents a baseline snapshot of test results.
/// Tracks which tests passed/failed in a previous run.
/// </summary>
public sealed class TestBaseline
{
  /// <summary>
  /// The timestamp when this baseline was created.
  /// </summary>
  [JsonPropertyName("timestamp")]
  public required DateTime Timestamp { get; init; }

  /// <summary>
  /// Map of test name to pass/fail state.
  /// </summary>
  [JsonPropertyName("testResults")]
  public required Dictionary<string, TestResult> TestResults { get; init; }

  /// <summary>
  /// Load baseline from file, or return empty baseline if file doesn't exist.
  /// </summary>
  public static TestBaseline LoadOrDefault(string filePath)
  {
    if (!File.Exists(filePath))
    {
      return CreateEmpty();
    }

    try
    {
      var json = File.ReadAllText(filePath);
      var baseline = JsonSerializer.Deserialize<TestBaseline>(json);
      return baseline ?? CreateEmpty();
    }
    catch
    {
      // If baseline file is corrupted, treat as empty
      return CreateEmpty();
    }
  }

  /// <summary>
  /// Save baseline to file.
  /// </summary>
  public void Save(string filePath)
  {
    var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(filePath, json);
  }

  /// <summary>
  /// Create an empty baseline.
  /// </summary>
  private static TestBaseline CreateEmpty()
  {
    return new TestBaseline
    {
      Timestamp = DateTime.UtcNow,
      TestResults = new Dictionary<string, TestResult>(),
    };
  }
}

/// <summary>
/// Represents the result of a single test.
/// </summary>
public sealed class TestResult
{
  /// <summary>
  /// Whether the test passed.
  /// </summary>
  [JsonPropertyName("passed")]
  public required bool Passed { get; init; }
}
