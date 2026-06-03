namespace MagicAST.Tests.Tests;

using System.Linq;
using MagicAST.Analysis;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR 0001 goal (b): a gold fixture must never carry an <c>IUnparsed</c> node
/// (<c>UnparsedAbility</c>/<c>UnparsedEffect</c>). A gold AST that embeds a parse
/// failure asserts the parser's current limitation as truth — the test-overfit
/// anti-pattern the TDD loop forbids.
///
/// <para>
/// <see cref="KnownUnparsedGold"/> is a self-cleaning burn-down allowlist of the
/// fixtures that predate this rule. The check is a ratchet: a fixture NOT on the
/// list must be unparsed-free, and a fixture ON the list must STILL contain an
/// unparsed node — so the moment one is hand-parsed properly, this test fails and
/// forces its removal from the list. The list can only shrink; new violations
/// are rejected outright.
/// </para>
/// </summary>
[TestFixture]
public class GoldFixtureUnparsedTests
{
  private static readonly IReadOnlySet<string> KnownUnparsedGold = new HashSet<string>(
    StringComparer.Ordinal
  )
  {
    // Partial: structured abilities alongside an unparsed ability/effect. The
    // 4 whole-card overfit fixtures (every ability unparsed) were deleted, not
    // listed — they had zero structured content to preserve.
    "BLB/WildcallSpree",
    "DGM/WearTear",
    "EOE/ChoraleoftheVoid",
    "EOE/ElegyAcolyte",
    "EOE/Hylderblade",
    "EOE/PlasmaBolt",
    "EOE/TragicTrajectory",
    "LRW/WindbriskHeights",
    "ONS/IxidorRealitySculptor",
    "ORI/WhirlerRogue",
    "SOM/PrecursorGolem",
    "TSP/AspectOfMongoose",
    "WHO/BillPotts",
    "WOE/CandyTrail",
  };

  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Gold_Contains_No_Unparsed_Nodes(CardTestCase testCase)
  {
    var name = testCase.Name.Replace('\\', '/');
    var unparsed = ResidualWalker.CountUnparsed(testCase.GetOutput());
    var total = unparsed.Values.Sum();

    if (KnownUnparsedGold.Contains(name))
    {
      Assert.That(
        total,
        Is.GreaterThan(0),
        $"'{name}' is on the KnownUnparsedGold burn-down list but no longer contains "
          + "unparsed nodes. Remove it from the list — goal (b) now holds for this fixture."
      );
      return;
    }

    var detail = string.Join(", ", unparsed.Select(kv => $"{kv.Key}×{kv.Value}"));
    Assert.That(
      total,
      Is.EqualTo(0),
      $"Gold fixture '{name}' contains unparsed nodes [{detail}]. Gold must never assert a "
        + "parse failure as truth (ADR 0001, goal b): hand-parse it properly, or remove the fixture."
    );
  }
}
