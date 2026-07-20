namespace MagicAST.Tests.Tests;

using MagicAtlas.Ast.Tests.Flows.Common;

/// <summary>
/// Pins the <b>near-duplicate relation itself</b> — the pure function behind the discriminator
/// governance report.
///
/// <para><b>What is and is not gated here.</b> Whether two near-duplicate discriminators are sprawl or
/// two real concepts is an architectural ruling, and ADR-0004 §1 routes rulings to prose rather than to a
/// data file a gate consumes. So the near-duplicate CHECK is a report
/// (<c>Data/_08_Reporting/discriminator-governance.json</c>, plus
/// <c>libs/magic-ast/scripts/lint-discriminators.py</c>), and its whitelist
/// (<c>discriminator-justifications.json</c>) is deleted — the rulings live as
/// <c>NearDuplicateOf</c>/<c>Reason</c> on the discriminator attribute, where they cannot outlive the
/// type they explain. The surviving GATE is <see cref="DiscriminatorUniquenessTests"/>, the hard
/// per-family collision check, which needs no whitelist because a genuine duplicate is always a
/// serialization bug.</para>
///
/// <para>The relation is still worth gating even though the check it feeds is not: it is a pure function
/// with no state, and a silent edit to it would make the report quietly stop seeing sprawl. These cases
/// carried over verbatim from the retired <c>DiscriminatorNearDuplicateTests</c>.</para>
/// </summary>
[TestFixture]
public class DiscriminatorNearnessTests
{
  [TestCase("dealDamage", "dealDamageToEach", true, TestName = "prefix stem")]
  [TestCase("tap", "untap", true, TestName = "levenshtein 2")]
  [TestCase("gift", "graft", true, TestName = "levenshtein 2, unrelated")]
  [TestCase("tap", "sacrifice", false, TestName = "unrelated, far apart")]
  [TestCase("draw", "drawCards", true, TestName = "stem at minimum length")]
  [TestCase("mill", "millCards", true, TestName = "stem at minimum length, 4 chars")]
  [TestCase("tap", "tapped", false, TestName = "stem shorter than the 4-char minimum")]
  [TestCase("exile", "exile", false, TestName = "identical is not near")]
  public void IsNear_classifies(string a, string b, bool expected) =>
    Assert.That(
      DiscriminatorNearness.IsNear(a, b),
      Is.EqualTo(expected),
      $"IsNear(\"{a}\", \"{b}\")"
    );

  /// <summary>Non-vacuity: the reflection half must actually find the vocabulary, and the relation must
  /// actually fire on it. A report over an empty set is worse than no report.</summary>
  [Test]
  public void The_report_has_something_to_report_on()
  {
    var declared = DiscriminatorNearness.All();
    Assert.Multiple(() =>
    {
      Assert.That(declared, Is.Not.Empty, "no discriminators reflected — the report would be vacuous");
      Assert.That(
        DiscriminatorNearness.NearPairs(declared),
        Is.Not.Empty,
        "no near-duplicate pairs at all — IsNear is likely broken, which would make the report vacuous"
      );
    });
  }

  /// <summary>Every declaration-site ruling must still describe a live near-duplicate, and must carry a
  /// reason. This is the one liveness hole the attribute cannot close structurally: a ruling dies with
  /// its OWN type automatically, but not when the type on the OTHER side of the pair is deleted.</summary>
  [Test]
  public void Declaration_site_rulings_are_live_and_reasoned()
  {
    var declared = DiscriminatorNearness.All();
    Assert.Multiple(() =>
    {
      Assert.That(
        DiscriminatorNearness.DeadRulings(declared),
        Is.Empty,
        "NearDuplicateOf name(s) whose counterpart is gone or no longer near — delete the ruling with it"
      );
      Assert.That(
        declared
          .Where(d => d.NearDuplicateOf.Count > 0 && string.IsNullOrWhiteSpace(d.Reason))
          .Select(d => $"{d.TypeName} (\"{d.Value}\")")
          .ToList(),
        Is.Empty,
        "NearDuplicateOf without a Reason — an unexplained explanation"
      );
    });
  }
}
