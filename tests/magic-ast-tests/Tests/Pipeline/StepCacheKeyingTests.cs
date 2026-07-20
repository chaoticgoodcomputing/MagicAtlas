using Flowthru.Step;
using MagicAST.Parsing;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;
using MagicAtlas.Ast.Tests.Infrastructure;
using NUnit.Framework;

namespace MagicAtlas.Ast.Tests.Tests.Pipeline;

/// <summary>
/// Gate for ADR 0004 (#22): Flowthru's step cache must be keyed on the code that performs
/// the transform, not only on the step class that declares it.
/// </summary>
/// <remarks>
/// <para>
/// The failure this guards against is silent. Flowthru's generated <c>CodeVersion</c> hashes
/// the step class's own source text only — its own XML docs say cross-assembly changes "are
/// not reflected". <c>ParseCorpusStep</c> is a wrapper around <c>MagicAST.OracleParser</c>,
/// so under the un-augmented key every parser change in this repo leaves ParseCorpus's cache
/// fingerprint identical, the cache plan reports FRESH, and the flow re-serves the previous
/// <c>parse-records.json</c>. Downstream that reads as a passing gate over a stale artifact —
/// the vacuous-pass mode ADR 0004 exists to close.
/// </para>
/// <para>
/// These are stateless invariants: no recorded baseline, no counts that only shrink. Each
/// asserts a property of the identity as the flow builder resolves it right now.
/// </para>
/// </remarks>
[TestFixture]
public class StepCacheKeyingTests
{
  [OneTimeSetUp]
  public void EnsureIdentityInstalled() => StepCodeIdentity.EnsureAugmented();

  /// <summary>
  /// THE red/green test. Every <c>[FlowthruStep]</c> class the harness declares must resolve
  /// to an identity that folds in its first-party code closure. Under the pre-fix keying the
  /// resolved identity is the bare generated source digest, with no closure component, and
  /// every step fails this assertion.
  /// </summary>
  /// <remarks>
  /// Total over the step population — no whitelist, because augmentation is unconditional.
  /// A step that genuinely reaches no first-party code still carries a closure digest (over
  /// the empty closure); non-vacuity for the steps that matter is asserted separately by
  /// <see cref="CodeClosure_ReachesTheAssemblyThatActuallyTransforms"/>.
  /// </remarks>
  [Test]
  public void EveryStepIdentity_FoldsInItsFirstPartyCodeClosure()
  {
    var stepTypes = StepCodeIdentity.EnumerateStepTypes().ToList();
    Assert.That(
      stepTypes,
      Is.Not.Empty,
      "No [FlowthruStep] classes were discovered — the sweep itself is broken, so every "
        + "other assertion in this fixture would pass vacuously."
    );

    var blind = new List<string>();
    foreach (var stepType in stepTypes)
    {
      var generated = StepCodeIdentity.GeneratedCodeVersion(stepType);
      if (generated is null) continue; // No recorded identity: Flowthru already treats it as uncacheable.

      var effective = StepCodeIdentity.EffectiveCodeVersion(stepType);
      var expected = generated + StepCodeIdentity.Separator + StepCodeIdentity.ClosureDigest(stepType);
      if (!string.Equals(effective, expected, StringComparison.Ordinal))
      {
        blind.Add($"{stepType.FullName}: resolved '{effective}', expected '{expected}'");
      }
    }

    Assert.That(
      blind,
      Is.Empty,
      "These steps resolve a cache identity blind to the code they execute. A change to that "
        + "code will not invalidate their cached output:\n  " + string.Join("\n  ", blind)
    );
  }

  /// <summary>
  /// Anti-vacuity guard. If the IL closure walk silently found nothing, every digest would be
  /// the hash of an empty closure — constant, and therefore just as code-blind as before,
  /// while <see cref="EveryStepIdentity_FoldsInItsFirstPartyCodeClosure"/> still passed. This
  /// pins the one case the whole issue is about: ParseCorpus must be keyed on MagicAST.
  /// </summary>
  [Test]
  public void CodeClosure_ReachesTheAssemblyThatActuallyTransforms()
  {
    var reached = StepCodeIdentity
      .ReachedAssemblies(typeof(ParseCorpusStep))
      .Select(a => a.GetName().Name)
      .ToList();

    Assert.That(
      reached,
      Does.Contain(typeof(OracleParser).Assembly.GetName().Name),
      "ParseCorpusStep's code closure does not reach MagicAST, the assembly that performs "
        + "the parse. Its cache key is therefore insensitive to every parser change. Reached: "
        + string.Join(", ", reached)
    );
  }

  /// <summary>
  /// The generated <c>CodeVersion</c> covers the step class's source only, so a same-assembly
  /// helper the step calls is invisible to it. <c>LossyParseAnalyzer</c> is exactly that: it
  /// decides <c>SuspectedLossy</c>/<c>DroppedTriggers</c> on every parse record, and editing
  /// it must invalidate the corpus parse. The IL walk has to descend past the step class for
  /// that to hold.
  /// </summary>
  [Test]
  public void CodeClosure_DescendsIntoSameAssemblyHelpers()
  {
    var reached = StepCodeIdentity.ReachedOwnAssemblyTypes(typeof(ParseCorpusStep));

    Assert.That(
      reached,
      Does.Contain(typeof(LossyParseAnalyzer).FullName),
      "ParseCorpusStep's closure does not include LossyParseAnalyzer, which it calls on every "
        + "card. Same-assembly helper edits would not invalidate the cached parse."
    );
  }

  /// <summary>
  /// An identity that changes for no reason is not safe, it is a disabled cache — every run
  /// pays the full cold cost. Recomputation must be byte-stable within a build.
  /// </summary>
  [Test]
  public void CodeClosure_IsStableAcrossRecomputation()
  {
    foreach (var stepType in StepCodeIdentity.EnumerateStepTypes())
    {
      var first = StepCodeIdentity.ClosureDigest(stepType);
      var second = StepCodeIdentity.ClosureDigest(stepType);
      Assert.That(
        second,
        Is.EqualTo(first),
        $"{stepType.FullName} produced an unstable closure digest — the cache would miss on "
          + "every run."
      );
    }
  }

  /// <summary>
  /// The identity must actually reach the flow. <c>FlowBuilder.AddStep</c> snapshots
  /// <c>CodeVersion</c> at wire-up time via <c>StepMetadataResolver</c>, so an augmentation
  /// installed after the flow is built would be inert. Building the real triage flow and
  /// reading the identity off the built step is what proves the wiring, not just the
  /// mechanism.
  /// </summary>
  [Test]
  public void BuiltFlow_CarriesTheCodeAwareIdentity()
  {
    var flow = MagicAstTriageFlow.Create(
      new Data.Catalog(Path.Combine(TestContext.CurrentContext.TestDirectory, "cache-keying-probe")),
      new HttpClient(),
      ratchetBaselinePath: "unused.json",
      handParsedFixturesRoot: "unused"
    );

    var parseCorpus = flow.Steps.Single(s => s.Label == "ParseCorpus");
    var expected =
      StepCodeIdentity.GeneratedCodeVersion(typeof(ParseCorpusStep))
      + StepCodeIdentity.Separator
      + StepCodeIdentity.ClosureDigest(typeof(ParseCorpusStep));

    Assert.That(
      parseCorpus.CodeVersion,
      Is.EqualTo(expected),
      "The built ParseCorpus step does not carry the code-aware identity — the augmentation "
        + "ran too late (or not at all) for FlowBuilder to observe it."
    );
  }
}
