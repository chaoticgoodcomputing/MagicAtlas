using NUnit.Framework;

/// <summary>
/// Assembly-level SetUpFixture for running code after all tests complete.
/// Must be public and outside of any namespace to apply to entire assembly.
/// </summary>
[SetUpFixture]
public class AssemblySetupFixture
{
  [OneTimeSetUp]
  public void RunBeforeAllTests()
  {
    TestContext.Progress.WriteLine("=== BEFORE ALL TESTS ===");
  }

  [OneTimeTearDown]
  public void RunAfterAllTests()
  {
    TestContext.Progress.WriteLine("=== AFTER ALL TESTS - STARTING SUMMARY ===");

    // Print failure hotspots for parser migration work
    var failureSummary = MagicAST.Tests.Infrastructure.FailureTracker.GetSummaryReport();
    TestContext.Progress.WriteLine(failureSummary);

    // Print ratchet test summary
    MagicAST.Tests.Infrastructure.RatchetTestTracker.Instance.PrintSummaryAndSetExitCode();
    TestContext.Progress.WriteLine("=== AFTER ALL TESTS - SUMMARY COMPLETE ===");
  }
}
