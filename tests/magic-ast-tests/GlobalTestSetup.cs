using MagicAtlas.Ast.Tests.Infrastructure;
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
  public void RunBeforeAllTests()
  {
    // Mirrors Program.Main. The NUnit host never runs Main, so the code-aware cache
    // identity has to be installed here too — otherwise the gate in
    // Tests/Pipeline/StepCacheKeyingTests.cs would be measuring an un-augmented registry
    // and would fail for the wrong reason.
    StepCodeIdentity.EnsureAugmented();
  }

  [OneTimeTearDown]
  public void RunAfterAllTests() { }
}
