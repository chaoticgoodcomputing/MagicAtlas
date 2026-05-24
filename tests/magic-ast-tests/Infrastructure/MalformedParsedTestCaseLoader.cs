namespace MagicAST.Tests.Infrastructure;

/// <summary>
/// Static loader for malformed/unparsed card test cases.
/// Provides NUnit TestCaseSource for MalformedParsedCardTests.
/// </summary>
public static class MalformedParsedTestCaseLoader
{
  private static readonly CardTestCaseLoader Loader = new("MalformedParsedCards");

  /// <summary>
  /// Gets all available test cases from the MalformedParsedCards directory.
  /// </summary>
  public static IEnumerable<CardTestCase> GetAllTestCases() => Loader.GetAllTestCases();

  /// <summary>
  /// Gets test case data for NUnit's TestCaseSource.
  /// </summary>
  public static IEnumerable<TestCaseData> GetTestCaseData() => Loader.GetTestCaseData();
}
