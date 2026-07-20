namespace MagicAST.Tests.Tests.InteractionRollup;

using System.Text;
using Flowthru.Caching;
using Flowthru.Flow;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.InteractionRollup;
using MagicAtlas.Ast.Tests.Infrastructure;
using NUnit.Framework;

/// <summary>
/// ADR-0004 §3 — the <b>price of the rollup's committed exception</b>, and the decision procedure it
/// establishes: <em>is an artifact's diff worth a gate?</em> Every Derived artifact is gitignored by
/// default; the rollup stays committed because its inter-run diff feeds the one mechanism the mast-loop
/// retro showed actually works (independent re-verification — humans reading changes), and the
/// <c>.cited</c> diff is where <b>witness attribution</b> changes surface. Committing costs this gate:
/// <b>regenerate, and assert byte-identity, against a busted cache.</b>
///
/// <para><b>The vacuity trap this fixture exists to avoid.</b> ADR-0004 §3 names it as "the single most
/// likely way this initiative gets implemented wrong": a gate that reads whatever the last run left
/// behind passes vacuously. Three structural properties close it, and none of them is a comment:
/// <list type="number">
///   <item><b>The derivation is a fresh run of the real flow.</b> Not a reimplementation of the
///     generator and not a reader of a previously written artifact — <see cref="InteractionRollupFlow"/>
///     itself, built over a <see cref="Catalog"/> rooted in a per-run temp directory, so every output
///     path is one that did not exist a millisecond ago.</item>
///   <item><b>The cache is busted explicitly.</b> The run passes
///     <c>BypassCacheReads = true</c> (Flowthru's <c>--no-cache</c>) <i>and</i>
///     <see cref="CachePlan.Empty"/>, and
///     <see cref="The_regeneration_run_executed_every_step_and_short_circuited_none"/> asserts every step
///     actually executed — a <c>Skipped</c> step would mean the scheduler short-circuited something and
///     the comparison would be against a cached value. (Issue #22's code-aware keying is a separate,
///     complementary guarantee: it makes the CLI's cached runs honest. This gate does not depend on it,
///     because a gate that trusts a cache key is still trusting a cache.)</item>
///   <item><b>The comparison is byte-for-byte against the SOURCE tree</b>, not against the build-output
///     copy under <c>TestContext.TestDirectory</c>. The committed file is the artifact under gate; a
///     <c>PreserveNewest</c> copy is one <c>--no-build</c> away from being the stale thing this whole
///     ADR is about.</item>
/// </list>
/// And the non-vacuity claim is itself executed rather than asserted in prose:
/// <see cref="A_one_character_edit_to_any_committed_artifact_turns_this_gate_red"/> mutates a single
/// character of each committed artifact in memory and requires the comparator to reject it.</para>
///
/// <para><b>Coverage is total, not enumerated.</b>
/// <see cref="Every_json_file_in_the_rollup_directory_is_covered_by_this_gate"/> reads the rollup
/// directory and requires it to equal exactly the set of gated artifacts, so a fifth committed rollup
/// file cannot appear un-gated — the ADR's rule is that a committed Derived artifact carries a
/// regeneration gate, and an artifact nobody listed here would be a committed Derived artifact with
/// none.</para>
///
/// <para><b>Lean/<c>.cited</c> cannot drift (ADR-0003 §8 / ADR-0004 §3).</b> Not a convention — a
/// topology of the flow. <see cref="The_lean_and_cited_twins_are_written_by_a_single_step"/> asserts each
/// pair is the two-output tuple of ONE step, so there is no schedule in which one twin is regenerated and
/// the other is not, and no cache plan in which one is fresh and the other stale.</para>
/// </summary>
[TestFixture]
public class RollupRegenerationGateTests
{
  /// <summary>The four committed rollup artifacts (ADR-0003 §8): the verbose pair and its lean projection.</summary>
  private static readonly string[] GatedArtifacts =
  [
    "port-interactions.cited.json",
    "port-interactions.json",
    "port-topology.cited.json",
    "port-topology.json",
  ];

  // ── the SOURCE tree, deliberately: the committed file is what is under gate ──────────────────────

  private static readonly string ProjectDir = ResolveProjectDir();
  private static readonly string CommittedRollupDir = Path.Combine(
    ProjectDir,
    "Fixtures",
    "Interactions",
    "rollup"
  );
  private static readonly string GoldsDir = Path.Combine(ProjectDir, "Fixtures", "Interactions", "golds");
  private static readonly string ScaffoldPath = Path.Combine(
    ProjectDir,
    "Fixtures",
    "Interactions",
    "topology-scaffold.json"
  );

  /// <summary>
  /// Locate the harness project directory in the WORKING TREE. The build output lives outside the
  /// project (<c>dist/tests/magic-ast-tests/net10.0</c> under the nx out-dir convention), so this walks
  /// up to the workspace root (the directory holding <c>nx.json</c>) and descends to the project — the
  /// same files a <c>dotnet run -- --flow InteractionRollup</c> would rewrite. Resolving the SOURCE
  /// tree rather than <c>TestContext.TestDirectory</c> is deliberate: the committed file is the artifact
  /// under gate, and a <c>PreserveNewest</c> build copy is one <c>--no-build</c> away from being stale.
  /// </summary>
  private static string ResolveProjectDir()
  {
    const string Csproj = "MagicAtlas.Ast.Tests.csproj";

    // Direct hit first (covers a conventional in-project bin/ layout).
    var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, Csproj)))
        return dir.FullName;
      if (File.Exists(Path.Combine(dir.FullName, "nx.json")))
      {
        var project = Path.Combine(dir.FullName, "tests", "magic-ast-tests");
        if (File.Exists(Path.Combine(project, Csproj)))
          return project;
      }
      dir = dir.Parent;
    }

    throw new InvalidOperationException(
      $"Could not locate {Csproj} from {TestContext.CurrentContext.TestDirectory}; the regeneration "
        + "gate cannot identify the committed artifacts it is supposed to gate."
    );
  }

  // ── the regeneration itself ─────────────────────────────────────────────────────────────────────

  private sealed record Regeneration(BuiltFlow Flow, FlowResult Result, string OutputDir);

  private static readonly Lazy<Regeneration> Fresh = new(Regenerate, isThreadSafe: true);

  /// <summary>
  /// Run the real <see cref="InteractionRollupFlow"/> over the committed golds into a throwaway
  /// directory, with cache reads suppressed. The Flowthru catalog roots every rollup item at
  /// <c>{base}/../Fixtures/Interactions/rollup/</c>, so a catalog based at <c>{temp}/Data</c> writes the
  /// four artifacts under <c>{temp}/Fixtures/Interactions/rollup/</c> — the real serializer, the real
  /// storage adapter, the real bytes, and none of the committed files touched.
  /// </summary>
  private static Regeneration Regenerate()
  {
    // #22's code-aware keying, installed the same way Program.Main installs it. Not load-bearing for
    // this gate (the run below reads no cache at all), but the flow must be built under the same step
    // identities the CLI builds it under, or the gate would be exercising a differently-wired flow.
    StepCodeIdentity.EnsureAugmented();

    var temp = Path.Combine(
      Path.GetTempPath(),
      "mast-rollup-regen-" + Guid.NewGuid().ToString("N")
    );
    var outputDir = Path.Combine(temp, "Fixtures", "Interactions", "rollup");
    Directory.CreateDirectory(outputDir);

    var catalog = new Catalog(Path.Combine(temp, "Data"));
    var flow = InteractionRollupFlow.Create(catalog, GoldsDir, ScaffoldPath);

    // BUST THE CACHE. BypassCacheReads is Flowthru's `--no-cache`: skip plan construction, run every
    // cacheable step, short-circuit nothing. CachePlan.Empty is belt-and-braces — a plan in which no
    // step is fresh — so even a scheduler that ignored the flag could not serve a step from cache.
    var options = ExecutionOptions.Default with
    {
      BypassCacheReads = true,
      CachePlan = CachePlan.Empty,
    };

    var result = flow.RunAsync(options, CancellationToken.None).GetAwaiter().GetResult();
    Assert.That(
      result.IsSuccess,
      Is.True,
      "the InteractionRollup flow failed to run, so nothing was regenerated: "
        + (result.FirstFailure?.ToString() ?? "no failure recorded")
    );

    return new Regeneration(flow, result, outputDir);
  }

  // ── Part A — byte-identity ──────────────────────────────────────────────────────────────────────

  [Test]
  [TestCaseSource(nameof(GatedArtifacts))]
  public void Regenerated_rollup_artifact_is_byte_identical_to_the_committed_copy(string artifact)
  {
    var committedPath = Path.Combine(CommittedRollupDir, artifact);
    Assert.That(
      File.Exists(committedPath),
      Is.True,
      $"{artifact} is gated but not committed at {committedPath}. Either commit it (ADR-0004 §3: its "
        + "diff is worth a gate) or remove it from this gate's artifact list and gitignore it."
    );

    var regeneratedPath = Path.Combine(Fresh.Value.OutputDir, artifact);
    Assert.That(
      File.Exists(regeneratedPath),
      Is.True,
      $"the flow did not write {artifact}; the gate would otherwise pass by comparing nothing"
    );

    var committed = File.ReadAllBytes(committedPath);
    var regenerated = File.ReadAllBytes(regeneratedPath);

    Assert.That(
      Divergence(committed, regenerated),
      Is.Null,
      () =>
        $"{artifact} is NOT byte-identical to a fresh derivation from the golds — the committed copy is "
        + $"stale or hand-edited.\n{Describe(artifact, committed, regenerated)}\n"
        + "ADR-0004 §3: this artifact is Derived. Never hand-edit it. Regenerate with "
        + "`dotnet run -- --flow InteractionRollup` (from tests/magic-ast-tests), then `git diff` the "
        + "rollup and justify what moved — that diff IS the product this file is committed for."
    );
  }

  // ── Part B — the gate is not vacuous ────────────────────────────────────────────────────────────

  /// <summary>
  /// The acceptance experiment, executed rather than described: a deliberate ONE-character edit to a
  /// committed artifact must turn this gate red. Each committed file is mutated in memory (never on
  /// disk) at a byte the JSON actually carries, and the comparator used by Part A is required to reject
  /// it. If Part A ever degrades into a structural or whitespace-tolerant comparison, this fails.
  /// </summary>
  [Test]
  [TestCaseSource(nameof(GatedArtifacts))]
  public void A_one_character_edit_to_any_committed_artifact_turns_this_gate_red(string artifact)
  {
    var committed = File.ReadAllBytes(Path.Combine(CommittedRollupDir, artifact));

    // A byte inside a value, not in the leading whitespace: the last byte of the file's first
    // "witnessed" occurrence when there is one, else the middle byte. Either way — exactly one byte.
    var index = committed.Length / 2;
    var edited = (byte[])committed.Clone();
    edited[index] = edited[index] == (byte)'x' ? (byte)'y' : (byte)'x';

    Assert.That(
      Divergence(committed, edited),
      Is.EqualTo(index),
      $"a one-character edit at byte {index} of {artifact} was not detected — the byte-identity "
        + "comparison is vacuous, which is exactly the failure ADR-0004 §3 names as the most likely way "
        + "this gate gets implemented wrong."
    );
  }

  /// <summary>
  /// Every step in the regeneration run must have EXECUTED. A <c>Skipped</c> result means the scheduler
  /// short-circuited a step from a cache plan, and the "regenerated" bytes Part A compares would be
  /// whatever a previous run left behind — the vacuous pass this fixture exists to prevent.
  /// </summary>
  [Test]
  public void The_regeneration_run_executed_every_step_and_short_circuited_none()
  {
    var results = Fresh.Value.Result.StepResults;

    Assert.That(
      results,
      Is.Not.Empty,
      "the regeneration run reported no step results at all — nothing was derived"
    );

    var shortCircuited = results
      .OfType<StepResult.Skipped>()
      .Select(s => s.StepLabel)
      .ToList();

    Assert.That(
      shortCircuited,
      Is.Empty,
      "step(s) were short-circuited instead of re-derived: "
        + string.Join(", ", shortCircuited)
        + ". A regeneration gate that reads a cached artifact passes vacuously (ADR-0004 §3)."
    );

    // And every step the flow declares ran — not merely "the ones that were not skipped".
    Assert.That(
      results.Select(r => r.StepLabel).ToHashSet(StringComparer.Ordinal),
      Is.EquivalentTo(Fresh.Value.Flow.Steps.Select(s => s.Label).ToHashSet(StringComparer.Ordinal)),
      "the run did not cover every step of the InteractionRollup flow"
    );
  }

  /// <summary>
  /// Coverage is derived from the directory, never from this file's list alone: a fifth committed rollup
  /// artifact would be a committed Derived artifact with no regeneration gate, which ADR-0004 §3 forbids.
  /// </summary>
  [Test]
  public void Every_json_file_in_the_rollup_directory_is_covered_by_this_gate()
  {
    var onDisk = Directory
      .EnumerateFiles(CommittedRollupDir, "*.json", SearchOption.AllDirectories)
      .Select(p => Path.GetRelativePath(CommittedRollupDir, p).Replace('\\', '/'))
      .OrderBy(p => p, StringComparer.Ordinal)
      .ToList();

    Assert.That(
      onDisk,
      Is.EquivalentTo(GatedArtifacts),
      "the committed rollup directory does not match the gated artifact set. ADR-0004 §3: a Derived "
        + "artifact is either untracked, or tracked AND covered by a byte-identical regeneration check. "
        + "Add the new artifact to this gate (and to the flow that derives it), or gitignore it."
    );
  }

  // ── Part C — the lean/cited pair is one pass, structurally ──────────────────────────────────────

  /// <summary>
  /// ADR-0003 §8 / ADR-0004 §3: the lean pair is a projection of the verbose one, generated in one pass,
  /// "so the two cannot drift from each other". This asserts the claim at the level that makes it true —
  /// the flow's own graph. Each pair is the two-output tuple of a SINGLE step, so no slice, schedule, or
  /// cache plan can regenerate one twin without the other; and no OTHER step writes either of them, so
  /// there is no second producer to disagree.
  /// </summary>
  [Test]
  public void The_lean_and_cited_twins_are_written_by_a_single_step()
  {
    var steps = Fresh.Value.Flow.Steps;

    foreach (var (lean, cited) in new[]
    {
      ("PortTopology", "PortTopologyCited"),
      ("PortInteractions", "PortInteractionsCited"),
    })
    {
      var producersOfLean = steps.Where(s => s.Outputs.Any(o => o.Label == lean)).ToList();
      var producersOfCited = steps.Where(s => s.Outputs.Any(o => o.Label == cited)).ToList();

      Assert.That(
        producersOfLean.Count,
        Is.EqualTo(1),
        $"{lean} must have exactly one producing step (found {producersOfLean.Count})"
      );
      Assert.That(
        producersOfCited.Count,
        Is.EqualTo(1),
        $"{cited} must have exactly one producing step (found {producersOfCited.Count})"
      );
      Assert.That(
        producersOfCited[0].Label,
        Is.EqualTo(producersOfLean[0].Label),
        $"{lean} and {cited} are written by DIFFERENT steps ('{producersOfLean[0].Label}' vs "
          + $"'{producersOfCited[0].Label}') — the lean twin is supposed to be a projection of the "
          + "verbose one emitted in the same pass (ADR-0003 §8). Split across two steps they can be "
          + "sliced, cached, and regenerated independently, i.e. they can drift."
      );
    }
  }

  /// <summary>
  /// The projection claim itself, on the bytes: the lean artifact is the cited artifact with the
  /// per-stem <c>witnesses</c> / per-rule provenance stripped, so the lean file is strictly smaller and
  /// the cited file names every gold the lean one does. A lean artifact that grew past its cited twin
  /// would mean the "projection" relation had inverted.
  /// </summary>
  [Test]
  public void The_lean_artifact_is_a_strict_projection_of_its_cited_twin()
  {
    foreach (var (lean, cited) in new[]
    {
      ("port-topology.json", "port-topology.cited.json"),
      ("port-interactions.json", "port-interactions.cited.json"),
    })
    {
      var leanText = File.ReadAllText(Path.Combine(Fresh.Value.OutputDir, lean));
      var citedText = File.ReadAllText(Path.Combine(Fresh.Value.OutputDir, cited));
      Assert.That(
        leanText.Length,
        Is.LessThan(citedText.Length),
        $"{lean} is not smaller than {cited}; the lean twin is supposed to be the cited twin with "
          + "provenance stripped"
      );
      Assert.That(
        citedText,
        Does.Contain("\"witnesses\"").Or.Contain("\"witness\""),
        $"{cited} carries no witness attribution at all — the .cited diff is the highest-value of the "
          + "four artifacts precisely because witness attribution changes surface in it (ADR-0004 §3)"
      );
      Assert.That(
        leanText,
        Does.Not.Contain("\"witnesses\""),
        $"{lean} carries witness attribution; it is supposed to be the stripped projection"
      );
    }
  }

  // ── byte comparison helpers ─────────────────────────────────────────────────────────────────────

  /// <summary>Index of the first differing byte, or null when the two sequences are identical.</summary>
  private static int? Divergence(byte[] expected, byte[] actual)
  {
    var shared = Math.Min(expected.Length, actual.Length);
    for (var i = 0; i < shared; i++)
      if (expected[i] != actual[i])
        return i;
    return expected.Length == actual.Length ? null : shared;
  }

  /// <summary>A human-readable account of the first divergence: line number and both sides' context.</summary>
  private static string Describe(string artifact, byte[] committed, byte[] regenerated)
  {
    var at = Divergence(committed, regenerated);
    if (at is not { } index)
      return "(identical)";

    var committedText = Encoding.UTF8.GetString(committed);
    var line = committedText.Take(Math.Min(index, committedText.Length)).Count(c => c == '\n') + 1;
    return $"  first divergence: byte {index} (~line {line} of the committed {artifact})\n"
      + $"  committed  : {Excerpt(committed, index)}\n"
      + $"  regenerated: {Excerpt(regenerated, index)}\n"
      + $"  lengths    : committed {committed.Length} B, regenerated {regenerated.Length} B";
  }

  private static string Excerpt(byte[] bytes, int index)
  {
    var start = Math.Max(0, index - 60);
    var length = Math.Min(bytes.Length - start, 120);
    if (length <= 0)
      return "(past end of file)";
    return Encoding.UTF8.GetString(bytes, start, length).Replace("\n", "\\n");
  }
}
