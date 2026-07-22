namespace MagicAST.Tests.Tests.ArtifactCensus;

using System.Diagnostics;
using MagicAtlas.Ast.Tests.Flows.ArtifactCensus;

/// <summary>
/// The <b>ADR-0004 §3 build-output GATE</b> (issue #23): a Derived artifact is a build output, so it
/// must not be tracked by git — unless it is one of the deliberate committed exceptions named below,
/// each of which pays for the exception with a gate of its own.
/// </summary>
/// <remarks>
/// <para><b>Why this is a gate and not a cleanup.</b> Gitignoring today's Derived artifacts is a
/// one-time edit; keeping the next one out is the actual property. ADR 0004 §3's argument is
/// structural — <i>"an artifact that does not exist in the repository cannot go stale in it"</i> — and
/// a structural argument only holds if nothing quietly re-commits one. So the invariant is checked
/// against the union of the census's Derived set and git's index, on every CORE-ring run.</para>
/// <para><b>Stateless, like its sibling.</b> No count, no baseline, no ratchet: the property is
/// membership ("this Derived artifact is untracked, or it is one of the named exceptions"). A newly
/// committed Derived artifact is red on its first run because it is <i>not in the exception table</i>,
/// not because a number moved. The exception table has liveness in both directions — an entry that
/// names something git no longer tracks, or something the census no longer calls Derived, is itself a
/// failure.</para>
/// <para><b>The decision procedure for the exception table</b> is ADR 0004 §3's single question:
/// <i>is its diff worth a gate?</i> Committing a Derived artifact buys exactly one thing — the
/// inter-run diff, which is what made the retro's re-verification loop work — and costs the risk of a
/// stale or hand-edited file. So an exception is admissible only when a gate already re-derives the
/// artifact and fails on divergence. Every entry below names that gate, and
/// <see cref="Exception_gates_exist"/> checks the named gate is a real type in this assembly rather
/// than a sentence somebody typed.</para>
/// </remarks>
[TestFixture]
public class DerivedArtifactTrackingGateTests
{
  private static readonly string Root = ArtifactClassifier.RepoRoot();
  private static readonly ArtifactClassifier.CensusResult Census = ArtifactClassifier.Run(Root);
  private static readonly IReadOnlySet<string> Tracked = TrackedFiles(Root);

  /// <summary>
  /// A committed Derived artifact: the path (or directory prefix), the reason it stays committed, and
  /// <b>exactly one</b> of two justifications.
  /// </summary>
  /// <param name="Gate">The NUnit type that re-derives the artifact and fails on divergence. This is
  /// the normal justification — ADR 0004 §3's price for committing a materialized view.</param>
  /// <param name="NotReproducibleBecause">The escape hatch for the one case a gate cannot cover: an
  /// artifact a clean checkout genuinely <i>cannot</i> rebuild, so there is nothing to re-derive it
  /// against. Naming it here is deliberately uncomfortable — it is a standing admission that the
  /// derivation base (external sources, Evidence, code) does not close for this file.</param>
  private sealed record CommittedException(
    string Path,
    string Why,
    string? Gate = null,
    string? NotReproducibleBecause = null
  )
  {
    /// <summary>True when <paramref name="candidate"/> is this entry, or lives under it when the
    /// entry names a directory (trailing <c>/</c>).</summary>
    public bool Covers(string candidate) =>
      Path.EndsWith('/')
        ? candidate.StartsWith(Path, StringComparison.Ordinal)
        : candidate.Equals(Path, StringComparison.Ordinal);
  }

  /// <summary>
  /// <b>HAND-MAINTAINED — the gate's whitelist.</b> The Derived artifacts that stay committed. Keep
  /// this SMALL: every entry is a materialized view someone has to keep fresh, which is the cost ADR
  /// 0004 §3 chose to stop paying by default.
  /// </summary>
  private static readonly CommittedException[] CommittedExceptions =
  [
    new(
      Path: "tests/magic-ast-tests/Fixtures/Interactions/rollup/",
      Why: "ADR 0004 §3's named exception. The rollup is the accretion loop's visible output — the "
        + "surface on which taxonomy drift is legible to a reviewer — and the .cited twins are where "
        + "witness attribution changes surface. Small, and its diff is a product in its own right. Pays "
        + "for the exception with a regeneration gate; issue #24 strengthens the contract check named "
        + "here into a byte-identity regeneration check.",
      Gate: "TopologyRollupContractTests"
    ),
    new(
      Path: "tests/magic-ast-tests/Tests/Interaction/Snapshots/",
      Why: "A snapshot baseline CANNOT be gitignored without destroying the thing it is for. The "
        + "committed snapshot IS the expectation the CORE-ring test diffs the live pipeline against; "
        + "regenerate it on demand and the test compares the engine to itself — the same vacuity ADR "
        + "0004 §5.2 rejects for the expected-tier pin, one layer down. Its inter-run diff is also the "
        + "only cross-pillar regression signal (a parser node-shape change silently dropping a port).",
      Gate: "PortWalkSentinelSnapshotTest"
    ),
    new(
      Path: "libs/magic-ast/schema/ast-schema.json",
      Why: "The published cross-language AST schema: consumed outside this repo's build, where 'run the "
        + "pipeline first' is not available. Re-exported from the node model by reflection and asserted "
        + "byte-fresh on every ring run, so a stale copy cannot land — `nx run magic-ast:schema` "
        + "regenerates it.",
      Gate: "SchemaExportTests"
    ),
    // libs/mast-interaction/known-coarse-projections.json — DELETED by issue #32. The blind-spot set is now
    // the derived backlog (ADR-0004 §2, Data/_08_Reporting/derived-backlog.json), and PortWalkExhaustivenessTests
    // re-derives it in-process rather than reading any committed file. No committed exception remains.
    new(
      Path: "libs/mtg-rules/Data/_03_Primary/Datasets/type-ontology.json",
      Why: "The one copyright-clean artifact of libs/mtg-rules, whose own .gitignore withholds every "
        + "other dataset in that project because they reproduce WotC rules prose verbatim. In ADR 0004 "
        + "§1 terms this is closer to an ingested EXTERNAL SOURCE (versioned by its fetch) than to a "
        + "build output; mast consumes it via `nx run mast:seed-ontology`.",
      NotReproducibleBecause:
        "its derivation input — the raw comprehensive-rules text — is deliberately not redistributable, "
        + "so no clean checkout can rebuild it and there is nothing for a gate to re-derive it against. "
        + "This is the one place the three-input derivation base does not close, and it is recorded here "
        + "rather than left implicit."
    ),
  ];

  /// <summary>The invariant itself, as a function of (census, git index) — so the teeth check can
  /// evaluate the REAL predicate against a perturbed world rather than a lookalike.</summary>
  private static List<string> Offenders(
    ArtifactClassifier.CensusResult census,
    IReadOnlySet<string> tracked
  ) =>
    census
      .Artifacts.Where(a => a.Kind == ArtifactClassifier.Derived)
      .Where(a => tracked.Contains(a.Path))
      .Where(a => !CommittedExceptions.Any(e => e.Covers(a.Path)))
      .Select(a => $"  COMMITTED  {a.Path}   ({a.Rule}: {a.Basis})")
      .ToList();

  /// <summary>THE GATE. No Derived artifact is tracked by git except the named exceptions.</summary>
  [Test]
  public void Derived_artifacts_are_not_committed()
  {
    var offenders = Offenders(Census, Tracked);

    Assert.That(
      offenders,
      Is.Empty,
      "ADR-0004 §3: Derived artifacts are build outputs — gitignored, reproduced by running the "
        + "Flowthru pipeline (Derived = f(external sources, Evidence, code)).\n\n"
        + string.Join("\n", offenders)
        + "\n\nFix by ONE of:\n"
        + "  (a) `git rm --cached <path>` and add it to a .gitignore — the default, and correct unless\n"
        + "      the artifact's inter-run DIFF is genuinely informative to a reviewer;\n"
        + "  (b) if the diff IS worth having, add it to CommittedExceptions naming the gate that\n"
        + "      re-derives it and fails on divergence. An exception without such a gate is not\n"
        + "      admissible: a committed file can be hand-edited or left stale, and that price is the\n"
        + "      whole reason the default flipped.\n"
    );
  }

  /// <summary>Liveness (1 of 2): an exception must still name a tracked artifact the census still calls
  /// Derived. An entry that outlived its artifact — or its Derived-ness — is exactly the stale
  /// hand-maintained claim this ADR exists to remove.</summary>
  [Test]
  public void Exceptions_are_live()
  {
    var derived = Census
      .Artifacts.Where(a => a.Kind == ArtifactClassifier.Derived)
      .Select(a => a.Path)
      .ToList();

    var dead = CommittedExceptions
      .Where(e => !derived.Any(p => e.Covers(p) && Tracked.Contains(p)))
      .Select(e => e.Path)
      .ToList();

    Assert.That(
      dead,
      Is.Empty,
      "CommittedExceptions names paths that are no longer both tracked AND classified Derived — the "
        + "exception has outlived its artifact. Remove the entry:\n"
        + string.Join("\n", dead.Select(p => "  " + p))
    );
  }

  /// <summary>Liveness (2 of 2): the gate each exception cites must exist. The exception's whole
  /// justification is "a gate re-derives this", so a citation that no longer resolves to a type turns
  /// the table into unchecked prose — the failure shape from ADR 0004's Context table.</summary>
  [Test]
  public void Exception_gates_exist()
  {
    var known = AppDomain
      .CurrentDomain.GetAssemblies()
      .SelectMany(TypesOf)
      .Select(t => t.Name)
      .ToHashSet(StringComparer.Ordinal);

    Assert.Multiple(() =>
    {
      foreach (var e in CommittedExceptions)
      {
        Assert.That(e.Why, Is.Not.Empty, $"CommittedExceptions[{e.Path}] must record a reason");
        Assert.That(
          (e.Gate is null) ^ (e.NotReproducibleBecause is null),
          Is.True,
          $"CommittedExceptions[{e.Path}] must carry EXACTLY one justification: a Gate that re-derives "
            + "it, or a NotReproducibleBecause stating why nothing can. Both is incoherent (a gate "
            + "implies a regenerator); neither is an unjustified exception."
        );

        if (e.Gate is null)
        {
          Assert.That(
            e.NotReproducibleBecause,
            Is.Not.Empty,
            $"CommittedExceptions[{e.Path}] claims irreproducibility without saying why"
          );
          continue;
        }

        Assert.That(
          known,
          Does.Contain(e.Gate),
          $"CommittedExceptions[{e.Path}] cites the gate '{e.Gate}', which is not a type in any loaded "
            + "assembly. Either the gate was renamed or removed — in which case the committed exception "
            + "has lost the thing that paid for it, and the artifact should be gitignored."
        );
      }
    });
  }

  /// <summary>
  /// <b>Teeth.</b> Proves the gate goes red when a Derived artifact is committed, rather than passing
  /// because the census found nothing Derived or because git reported an empty index. Uses the
  /// reporting layer — anything under <c>_08_Reporting</c> classifies Derived by the project's own
  /// layering convention — and stages it into git's index (staging is enough: <c>git ls-files</c>
  /// reports the index, which is what "committed" means for a pre-commit gate) before backing it out.
  /// </summary>
  [Test]
  public void Gate_detects_a_committed_derived_artifact()
  {
    var probeName = $"__derived-tracking-gate-probe-{Guid.NewGuid():N}" + ".jso" + "n";
    var probeRel = "tests/magic-ast-tests/Data/_08_Reporting/" + probeName;
    var probe = Path.Combine(Root, probeRel.Replace('/', Path.DirectorySeparatorChar));

    Directory.CreateDirectory(Path.GetDirectoryName(probe)!);
    File.WriteAllText(probe, "{ \"_probe\": \"ADR-0004 #23 gate teeth check\" }\n");
    try
    {
      // -f because the whole layer is gitignored — which is the point: the gate must catch a file
      // someone force-added past the ignore rule, not merely trust .gitignore to be complete.
      Git(Root, "add", "-f", "--", probeRel);

      var perturbed = ArtifactClassifier.Run(Root);
      var tracked = TrackedFiles(Root);

      Assert.Multiple(() =>
      {
        Assert.That(
          perturbed.Artifacts.Any(a => a.Path == probeRel && a.Kind == ArtifactClassifier.Derived),
          Is.True,
          "the probe did not classify as Derived — the teeth check is not exercising the gate"
        );
        Assert.That(tracked, Does.Contain(probeRel), "git did not report the staged probe as tracked");
        Assert.That(
          Offenders(perturbed, tracked).Any(o => o.Contains(probeRel, StringComparison.Ordinal)),
          Is.True,
          "committing a Derived artifact did NOT make the gate red — the invariant has no teeth."
        );
      });
    }
    finally
    {
      Git(Root, "rm", "--cached", "--force", "--quiet", "--", probeRel);
      if (File.Exists(probe))
        File.Delete(probe);
    }

    Assert.That(
      TrackedFiles(Root),
      Does.Not.Contain(probeRel),
      "the probe was not fully unstaged — clean it up with `git rm --cached`"
    );
  }

  // ── git ──────────────────────────────────────────────────────────────────────────────────────────

  /// <summary>Every path git tracks, repo-relative and forward-slashed. <c>git ls-files</c> reports the
  /// INDEX, so a staged-but-uncommitted file counts — the right reading for a gate meant to stop a
  /// Derived artifact from being committed in the first place.</summary>
  private static IReadOnlySet<string> TrackedFiles(string root) =>
    Git(root, "ls-files", "-z")
      .Split('\0', StringSplitOptions.RemoveEmptyEntries)
      .Select(p => p.Replace('\\', '/'))
      .ToHashSet(StringComparer.Ordinal);

  private static string Git(string root, params string[] args)
  {
    var psi = new ProcessStartInfo("git")
    {
      WorkingDirectory = root,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
    };
    foreach (var a in args)
      psi.ArgumentList.Add(a);

    using var proc =
      Process.Start(psi)
      ?? throw new InvalidOperationException(
        "could not start `git` — this gate reads the index, so git must be on PATH"
      );
    var stdout = proc.StandardOutput.ReadToEnd();
    var stderr = proc.StandardError.ReadToEnd();
    proc.WaitForExit();

    if (proc.ExitCode != 0)
      throw new InvalidOperationException(
        $"`git {string.Join(' ', args)}` failed ({proc.ExitCode}): {stderr}"
      );
    return stdout;
  }

  private static IEnumerable<Type> TypesOf(System.Reflection.Assembly asm)
  {
    try
    {
      return asm.GetTypes();
    }
    catch (System.Reflection.ReflectionTypeLoadException e)
    {
      return e.Types.Where(t => t is not null)!;
    }
  }
}
