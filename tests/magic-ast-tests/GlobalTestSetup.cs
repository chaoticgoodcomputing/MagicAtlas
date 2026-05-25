using NUnit.Framework;

/// <summary>
/// Assembly-level SetUpFixture. Kept as a stub for future assembly-wide
/// setup; the ratchet-driven progress reporting that previously lived here
/// has been removed in favour of vanilla NUnit pass/fail.
/// </summary>
[SetUpFixture]
public class AssemblySetupFixture
{
  [OneTimeSetUp]
  public void RunBeforeAllTests() { }

  [OneTimeTearDown]
  public void RunAfterAllTests() { }
}
