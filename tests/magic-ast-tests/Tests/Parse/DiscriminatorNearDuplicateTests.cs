namespace MagicAST.Tests.Tests;

using System.Text.Json;
using MagicAST.Schema;
using MagicAtlas.Ast.Tests.Flows.ArtifactCensus;

/// <summary>
/// The <b>soft</b> half of discriminator governance (alignment initiative 02), promoted into the CORE
/// ring. <see cref="DiscriminatorUniquenessTests"/> covers the hard half (a genuine per-base collision);
/// this covers near-duplicates — two values in one base within Levenshtein ≤ 2, or where one is a
/// prefix-stem of the other. Those are the sprawl shape: <c>dealDamageToEach</c> landing beside
/// <c>dealDamage</c> because a worker did not know the existing one was there.
///
/// <para><b>Why this is a test and not just the script.</b> The invariant was previously enforced only by
/// <c>libs/magic-ast/scripts/lint-discriminators.py</c>, invoked by hand from the mast-loop skill's
/// per-merge-group checklist — so it depended on an agent following a procedure, and nothing failed when
/// one didn't. It had in fact been failing for some time without stopping anything. Enforcement belongs on
/// the ring; the script stays as the loop's convenience for worktrees where <c>dotnet</c> is unavailable,
/// exactly as <see cref="DiscriminatorUniquenessTests"/> describes for the hard half.</para>
///
/// <para><b>Stateless, per the standing rule.</b> There is no baseline and no notion of a "new"
/// discriminator. The retired <c>discriminator-baseline.json</c> was a debt baseline: it grandfathered
/// everything already present and only asked about the delta, and it drifted — 330 committed entries
/// against 364 in source, leaving 34 permanently "new". Refreshing it would have been worse than the
/// drift, since a baseline regenerated from current source makes every pair not-new and the check
/// vacuous. The only state is the explicit named whitelist below.</para>
///
/// <para><b>On the duplicated algorithm.</b> The near-ness rule now exists in C# here and in Python in
/// the script. That is a real duplication and the honest statement is that <b>this test is the
/// enforcement point</b> — if the two ever disagree, the ring is right and the script is stale. The
/// shared whitelist file keeps them anchored in practice: a justification the script does not need would
/// show up here as a dead entry and fail <see cref="Every_justification_explains_a_real_near_duplicate"/>.
/// </para>
/// </summary>
[TestFixture]
public class DiscriminatorNearDuplicateTests
{
  private const int LevenshteinMax = 2;
  private const int StemMinLength = 4;

  private sealed record Justification(string Name, string Near, string Reason);

  // Resolved by walking up for the workspace marker rather than counting `..` segments, so it survives
  // a change in output layout. The whitelist lives beside the source it governs, not in the test project.
  private static readonly string JustificationsPath = Path.Combine(
    ArtifactClassifier.RepoRoot(),
    "libs",
    "magic-ast",
    "schema",
    "discriminator-justifications.json"
  );

  /// <summary>The explained pairs, matched symmetrically (a justification explains a pair either way).</summary>
  private static HashSet<(string, string)> Justified()
  {
    var entries = JsonSerializer.Deserialize<List<Justification>>(
      File.ReadAllText(JustificationsPath),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    )!;
    var pairs = new HashSet<(string, string)>();
    foreach (var e in entries.Where(e => !string.IsNullOrEmpty(e.Name) && !string.IsNullOrEmpty(e.Near)))
    {
      pairs.Add((e.Name, e.Near));
      pairs.Add((e.Near, e.Name));
    }
    return pairs;
  }

  private static int Levenshtein(string a, string b)
  {
    a = a.ToLowerInvariant();
    b = b.ToLowerInvariant();
    if (a == b)
      return 0;
    if (a.Length == 0)
      return b.Length;
    if (b.Length == 0)
      return a.Length;

    var prev = Enumerable.Range(0, b.Length + 1).ToArray();
    for (var i = 1; i <= a.Length; i++)
    {
      var cur = new int[b.Length + 1];
      cur[0] = i;
      for (var j = 1; j <= b.Length; j++)
      {
        var cost = a[i - 1] == b[j - 1] ? 0 : 1;
        cur[j] = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
      }
      prev = cur;
    }
    return prev[b.Length];
  }

  /// <summary>One value is a prefix-stem of the other (<c>dealDamage</c> / <c>dealDamageToEach</c>).</summary>
  private static bool SharesStem(string a, string b)
  {
    var x = a.ToLowerInvariant();
    var y = b.ToLowerInvariant();
    if (x == y)
      return false;
    var (shorter, longer) = x.Length <= y.Length ? (x, y) : (y, x);
    return shorter.Length >= StemMinLength && longer.StartsWith(shorter, StringComparison.Ordinal);
  }

  private static bool IsNear(string a, string b) =>
    a != b && (Levenshtein(a, b) <= LevenshteinMax || SharesStem(a, b));

  /// <summary>Every intra-base near-duplicate pair in the current schema, each pair once.</summary>
  private static List<(string Base, string A, string B)> NearPairs()
  {
    var pairs = new List<(string, string, string)>();
    foreach (var b in SchemaExport.Build().Bases)
    {
      var values = b.Types.Select(t => t.Discriminator).Distinct().OrderBy(v => v, StringComparer.Ordinal).ToList();
      for (var i = 0; i < values.Count; i++)
        for (var j = i + 1; j < values.Count; j++)
          if (IsNear(values[i], values[j]))
            pairs.Add((b.DiscriminatorKey, values[i], values[j]));
    }
    return pairs;
  }

  /// <summary>THE GATE. Every near-duplicate pair must be renamed away or explicitly justified.</summary>
  [Test]
  public void Every_near_duplicate_pair_is_justified()
  {
    var justified = Justified();
    var unexplained = NearPairs()
      .Where(p => !justified.Contains((p.A, p.B)))
      .Select(p => $"[{p.Base}] \"{p.A}\" ~ \"{p.B}\"")
      .ToList();

    Assert.That(
      unexplained,
      Is.Empty,
      "Unjustified near-duplicate discriminator(s) — rename one, or add a {name, near, reason} entry to "
        + "libs/magic-ast/schema/discriminator-justifications.json explaining why both must exist "
        + "(initiative 02):\n  "
        + string.Join("\n  ", unexplained)
    );
  }

  /// <summary>
  /// The whitelist must stay live. A justification whose pair no longer near-collides — because one side
  /// was renamed or deleted — is stale state accumulating in the very file that is supposed to be the
  /// honest record, and would silently pre-authorize a future collision on that name.
  /// </summary>
  [Test]
  public void Every_justification_explains_a_real_near_duplicate()
  {
    var live = NearPairs().Select(p => (p.A, p.B)).ToHashSet();
    var entries = JsonSerializer.Deserialize<List<Justification>>(
      File.ReadAllText(JustificationsPath),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    )!;

    var dead = entries
      .Where(e => !live.Contains((e.Name, e.Near)) && !live.Contains((e.Near, e.Name)))
      .Select(e => $"\"{e.Name}\" ~ \"{e.Near}\"")
      .ToList();

    Assert.That(
      dead,
      Is.Empty,
      "Justification(s) for pairs that no longer near-collide — delete them; a whitelist that outlives "
        + "its subject is exactly the stale state the baseline was retired for:\n  "
        + string.Join("\n  ", dead)
    );
  }

  /// <summary>
  /// Non-vacuity. If the schema or the whitelist path ever went empty, both gates above would pass while
  /// checking nothing — the failure mode this whole initiative exists to prevent.
  /// </summary>
  [Test]
  public void The_gate_has_something_to_check()
  {
    Assert.Multiple(() =>
    {
      Assert.That(SchemaExport.Build().Bases, Is.Not.Empty, "no discriminator bases — the gate is vacuous");
      Assert.That(Justified(), Is.Not.Empty, "no justifications loaded — the whitelist path is likely wrong");
      Assert.That(
        NearPairs(),
        Is.Not.Empty,
        "no near-duplicate pairs detected at all — IsNear is likely broken, which would make the gate vacuous"
      );
    });
  }

  /// <summary>The near-ness rule itself, pinned — the half most likely to rot silently if edited.</summary>
  [TestCase("dealDamage", "dealDamageToEach", true, TestName = "prefix stem")]
  [TestCase("tap", "untap", true, TestName = "levenshtein 2")]
  [TestCase("gift", "graft", true, TestName = "levenshtein 2, unrelated")]
  [TestCase("tap", "sacrifice", false, TestName = "unrelated, far apart")]
  [TestCase("draw", "drawCards", true, TestName = "stem at minimum length")]
  [TestCase("mill", "millCards", true, TestName = "stem at minimum length, 4 chars")]
  [TestCase("tap", "tapped", false, TestName = "stem shorter than the 4-char minimum")]
  [TestCase("exile", "exile", false, TestName = "identical is not near")]
  public void IsNear_classifies(string a, string b, bool expected) =>
    Assert.That(IsNear(a, b), Is.EqualTo(expected), $"IsNear(\"{a}\", \"{b}\")");
}
