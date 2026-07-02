namespace MagicAST.Tests.Infrastructure;

/// <summary>
/// Static loader for hand-parsed card test cases.
/// Provides NUnit TestCaseSource for HandParsedCardTests.
/// </summary>
public static class HandParsedTestCaseLoader
{
  private static readonly CardTestCaseLoader Loader = new("HandParsedCards");

  /// <summary>
  /// Gets all available test cases from the HandParsedCards directory.
  /// </summary>
  public static IEnumerable<CardTestCase> GetAllTestCases() => Loader.GetAllTestCases();

  /// <summary>
  /// Gets test case data for NUnit's TestCaseSource.
  /// </summary>
  public static IEnumerable<TestCaseData> GetTestCaseData() => Loader.GetTestCaseData();
}
