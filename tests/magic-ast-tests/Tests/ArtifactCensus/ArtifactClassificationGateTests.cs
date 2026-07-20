namespace MagicAST.Tests.Tests.ArtifactCensus;

using MagicAtlas.Ast.Tests.Flows.ArtifactCensus;

/// <summary>
/// The <b>ADR-0004 §1 classification GATE</b>: every artifact on the declared surface must be
/// classified. An unclassified artifact fails the build.
/// </summary>
/// <remarks>
/// <para><b>Stateless by construction.</b> This gate does not read
/// <c>_08_Reporting/artifact-census.json</c> — that manifest is a gitignored Flowthru output and would
/// not exist on a clean checkout. It re-runs the same pure <see cref="ArtifactClassifier"/> the census
/// flow runs, over the live working tree. There is no baseline, no count, and nothing that ratchets:
/// the invariant is a <i>membership</i> property ("this artifact is resolved by a rule, or a human has
/// explicitly acknowledged that it is ambiguous"), so a newly-added unclassified artifact is red on the
/// very first run and stays red until someone rules on it.</para>
/// <para><b>The whitelist has liveness.</b> <see cref="ArtifactClassifier.AcknowledgedAmbiguous"/> and
/// <see cref="ArtifactClassifier.HumanRulings"/> are checked back against reality: an entry for a
/// deleted file, or for an artifact a rule now resolves on its own, is itself a failure. That is what
/// stops the whitelist from becoming the next hand-maintained artifact that quietly stopped being true.</para>
/// </remarks>
[TestFixture]
public class ArtifactClassificationGateTests
{
  private static readonly string Root = ArtifactClassifier.RepoRoot();
  private static readonly ArtifactClassifier.CensusResult Census = ArtifactClassifier.Run(Root);

  /// <summary>THE GATE. Every discovered artifact carries a classification — one of the three ADR §1
  /// kinds, or an explicitly acknowledged <c>needs-human-classification</c>.</summary>
  [Test]
  public void Every_artifact_is_classified()
  {
    var unclassified = Census.Unclassified;

    Assert.That(
      unclassified,
      Is.Empty,
      "ADR-0004 §1: every artifact must be classified.\n\n"
        + string.Join(
          "\n",
          unclassified.Select(a => $"  UNCLASSIFIED  {a.Path}")
        )
        + "\n\nNo classification rule resolved these, and no human has acknowledged them. Fix by ONE of:\n"
        + "  (a) give the artifact a real regeneration path (a Flowthru catalog output, or a named writer\n"
        + "      in product source) — it then classifies itself as Derived;\n"
        + "  (b) move it under a hand-authored gold directory — it then classifies itself as Evidence;\n"
        + "  (c) if a human has ruled on it, add it to ArtifactClassifier.HumanRulings with the basis;\n"
        + "  (d) if it is genuinely ambiguous, add it to ArtifactClassifier.AcknowledgedAmbiguous with a\n"
        + "      short note on WHY it is ambiguous. Flagging is a success state; guessing is not.\n"
    );
  }

  /// <summary>The census must actually see the repository. A scanner that silently found nothing would
  /// pass the gate above vacuously — the exact "the report WAS the bug" failure ADR 0004 was written
  /// after.</summary>
  [Test]
  public void Census_surface_is_non_vacuous()
  {
    Assert.Multiple(() =>
    {
      Assert.That(Census.Artifacts, Is.Not.Empty, "the census found no artifacts at all");
      Assert.That(
        Census.Artifacts.Count(a => a.Kind == ArtifactClassifier.Evidence),
        Is.GreaterThan(100),
        "the hand-authored gold surface (HandParsedCards et al.) went missing from the census"
      );
      Assert.That(
        Census.Roots.Where(r => r.Exists).Select(r => r.Path),
        Does.Contain("tests/magic-ast-tests/Fixtures"),
        "the primary fixture scan root was not found"
      );
    });
  }

  /// <summary>Liveness for the acknowledgment whitelist: every acknowledged path must still exist AND
  /// still be unresolved. An acknowledgment that outlived its artifact — or its ambiguity — is stale
  /// hand-maintained state, which is the thing this ADR exists to kill.</summary>
  [Test]
  public void Acknowledgments_are_live()
  {
    var byPath = Census.Artifacts.ToDictionary(a => a.Path, StringComparer.Ordinal);

    var dangling = ArtifactClassifier
      .AcknowledgedAmbiguous.Keys.Where(p => !byPath.ContainsKey(p))
      .ToList();
    var resolved = ArtifactClassifier
      .AcknowledgedAmbiguous.Keys.Where(p =>
        byPath.TryGetValue(p, out var a) && a.Kind != ArtifactClassifier.NeedsHuman
      )
      .ToList();

    Assert.Multiple(() =>
    {
      Assert.That(
        dangling,
        Is.Empty,
        "AcknowledgedAmbiguous names artifacts that no longer exist on the scan surface — remove them:\n"
          + string.Join("\n", dangling.Select(p => "  " + p))
      );
      Assert.That(
        resolved,
        Is.Empty,
        "AcknowledgedAmbiguous names artifacts a rule now resolves on its own — the ambiguity is gone, "
          + "so drop the acknowledgment:\n"
          + string.Join("\n", resolved.Select(p => "  " + p))
      );
    });
  }

  /// <summary>Liveness for the human-ruling table: every ruling must name a real artifact and one of the
  /// three ADR §1 kinds (a ruling of "needs-human" is not a ruling).</summary>
  [Test]
  public void Human_rulings_are_live_and_well_formed()
  {
    string[] valid =
    [
      ArtifactClassifier.Evidence,
      ArtifactClassifier.Derived,
      ArtifactClassifier.ArchitecturalDecision,
    ];
    var paths = Census.Artifacts.Select(a => a.Path).ToHashSet(StringComparer.Ordinal);

    Assert.Multiple(() =>
    {
      foreach (var (path, ruling) in ArtifactClassifier.HumanRulings)
      {
        Assert.That(paths, Does.Contain(path), $"HumanRulings names a missing artifact: {path}");
        Assert.That(valid, Does.Contain(ruling.Kind), $"HumanRulings[{path}] has an invalid kind");
        Assert.That(
          ruling.Basis,
          Is.Not.Empty,
          $"HumanRulings[{path}] must record the basis for the ruling"
        );
      }
      Assert.That(
        ArtifactClassifier.HumanRulings.Keys.Intersect(
          ArtifactClassifier.AcknowledgedAmbiguous.Keys,
          StringComparer.Ordinal
        ),
        Is.Empty,
        "an artifact cannot be both ruled on and acknowledged as ambiguous"
      );
    });
  }

  /// <summary>The second key on rule 5: every directory NAMED as wholly generated must exist and be
  /// independently confirmed by the source index as having a regenerator. Without this the named
  /// allowlist would be exactly the kind of unchecked claim ADR 0004 was written after.</summary>
  [Test]
  public void Generated_directories_are_confirmed_by_source()
  {
    Assert.That(
      Census.UnconfirmedGeneratedDirectories,
      Is.Empty,
      "ArtifactClassifier.GeneratedDirectories names directories that no longer exist, or whose "
        + "regenerator the source index can no longer find:\n"
        + string.Join("\n", Census.UnconfirmedGeneratedDirectories.Select(d => "  " + d))
    );
  }

  /// <summary>
  /// <b>Teeth.</b> Proves the gate actually goes red when an unclassified artifact appears, rather than
  /// passing because the scanner is broken or the whitelist is load-bearing in the wrong direction.
  /// Drops a throwaway file into a scan root that no convention covers, re-runs the classifier, and
  /// asserts it surfaces as unclassified — then removes it. Mirrors the perturbation self-test idiom
  /// already used by the port-walk snapshot suite.
  /// </summary>
  [Test]
  public void Gate_detects_a_new_unclassified_artifact()
  {
    // The probe filename must NOT appear as a literal anywhere in source — including in this test.
    // (It did, in the first cut, and the classifier promptly and correctly classified the probe as
    // Derived-by-NamedWriter off this very method. A satisfying way to learn that rule 4 works.)
    var probeName = $"__artifact-census-gate-probe-{Guid.NewGuid():N}" + ".jso" + "n";
    var probeRel = "tests/magic-ast-tests/Fixtures/" + probeName;
    var probe = Path.Combine(Root, "tests", "magic-ast-tests", "Fixtures", probeName);

    Assert.That(
      File.Exists(probe),
      Is.False,
      "the gate probe path is occupied — a previous run leaked it; delete it and re-run"
    );

    try
    {
      File.WriteAllText(probe, "{ \"_probe\": \"ADR-0004 #21 gate teeth check\" }\n");

      var perturbed = ArtifactClassifier.Run(Root);

      Assert.That(
        perturbed.Unclassified.Select(a => a.Path),
        Does.Contain(probeRel),
        "adding a brand-new artifact with no regeneration path, no gold convention and no acknowledgment "
          + "did NOT make the gate red — the classification invariant has no teeth."
      );
    }
    finally
    {
      if (File.Exists(probe))
        File.Delete(probe);
    }

    // And the working tree is back to green once the probe is gone.
    Assert.That(
      ArtifactClassifier.Run(Root).Unclassified,
      Is.Empty,
      "the probe was not fully cleaned up"
    );
  }
}
