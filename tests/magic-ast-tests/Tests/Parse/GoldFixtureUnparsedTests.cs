namespace MagicAST.Tests.Tests;

using System.Linq;
using System.Text.Json;
using MagicAST.Analysis;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// ADR 0001 goal (b): a gold fixture must never carry an <c>IUnparsed</c> node
/// (<c>UnparsedAbility</c>/<c>UnparsedEffect</c>). A gold AST that embeds a parse
/// failure asserts the parser's current limitation as truth — the test-overfit
/// anti-pattern the TDD loop forbids.
///
/// <para>
/// The exceptions are an explicit, NAMED whitelist — <c>Fixtures/whitelist-unparsed.json</c>, each entry
/// a card + tag (debt|irreducible) + reason. A fixture NOT on the whitelist must be unparsed-free (new
/// debt is rejected outright, loudly); a fixture ON it must STILL contain an unparsed node, so the moment
/// one is hand-parsed properly this test fails and forces its removal. The whitelist holds only named,
/// justified carve-outs and only shrinks — TARGET: empty (see <c>libs/magic-ast/docs/gold-burndown-plan.md</c>).
/// De-ratcheted from the in-test <c>KnownUnparsedGold</c> HashSet 2026-06-16.
/// </para>
/// </summary>
[TestFixture]
public class GoldFixtureUnparsedTests
{
  // The explicit unparsed whitelist (Fixtures/whitelist-unparsed.json) as the set of permitted card names.
  private static readonly Lazy<IReadOnlySet<string>> KnownUnparsedGold = new(LoadUnparsedWhitelist);

  private static IReadOnlySet<string> LoadUnparsedWhitelist()
  {
    var path = System.IO.Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "whitelist-unparsed.json"
    );
    var set = new HashSet<string>(StringComparer.Ordinal);
    if (!System.IO.File.Exists(path))
    {
      return set;
    }

    using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
    {
      foreach (var e in entries.EnumerateArray())
      {
        if (e.TryGetProperty("card", out var c) && c.GetString() is { } name)
        {
          set.Add(name);
        }
      }
    }

    return set;
  }

  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Gold_Contains_No_Unparsed_Nodes(CardTestCase testCase)
  {
    var name = testCase.Name.Replace('\\', '/');
    var unparsed = ResidualWalker.CountUnparsed(testCase.GetOutput());
    var total = unparsed.Values.Sum();

    if (KnownUnparsedGold.Value.Contains(name))
    {
      Assert.That(
        total,
        Is.GreaterThan(0),
        $"'{name}' is on the unparsed whitelist (Fixtures/whitelist-unparsed.json) but no longer "
          + "contains unparsed nodes. Remove its entry — goal (b) now holds for this fixture (the "
          + "whitelist only shrinks)."
      );
      return;
    }

    var detail = string.Join(", ", unparsed.Select(kv => $"{kv.Key}×{kv.Value}"));
    Assert.That(
      total,
      Is.EqualTo(0),
      $"Gold fixture '{name}' contains unparsed nodes [{detail}]. Gold must never assert a "
        + "parse failure as truth (ADR 0001, goal b): hand-parse it properly (or delete a synthetic "
        + "fixture). Only if genuinely irreducible, add a justified entry to "
        + "Fixtures/whitelist-unparsed.json — new debt is otherwise rejected outright."
    );
  }
}
