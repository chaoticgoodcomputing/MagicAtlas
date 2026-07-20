namespace MagicAST.Tests.Tests;

using System.Text.Encodings.Web;
using System.Text.Json;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Item R3 — the automated drift SWEEP: proposes <c>oracle-text-quarantine.json</c> add/remove entries
/// from a fresh comparison against the authoritative sources, mirroring <see cref="GoldRegenerationUtility"/>'s
/// established idiom (corpus-fed maintenance utility, <c>[Explicit]</c>, run on demand) and
/// <see cref="GoldOracleTextFidelityTests"/>'s two-source authoritative-text ladder
/// (<c>card-inputs.json</c>, else the full <c>oracle-cards.json</c> bulk) and its <c>Norm()</c> comparison.
///
/// <para>
/// Per the governing principle — growth of a hand-curated list may be code-DETECTED (a sweep proposes),
/// but the ACT of adding/removing an exemption stays a human-confirmed decision — this utility NEVER
/// writes to <c>oracle-text-quarantine.json</c>. It only emits
/// <c>Data/_08_Reporting/quarantine-sweep-proposal.json</c> (gitignored, regenerated on demand): an
/// <b>ADD</b> list (a gold drifts from its authoritative text but isn't quarantined — a newly-landed
/// errata/reprint, or a gold that was simply wrong) with a before/after text diff per entry, and a
/// <b>REMOVE</b> list (a gold IS quarantined but now matches — the shrink signal, e.g. after a
/// <see cref="GoldRegenerationUtility"/> re-point). Acting on either list is a separate, deliberate,
/// human-reviewed step.
/// </para>
///
/// <para>
/// Corpus-gated like the fidelity test itself: <c>card-inputs.json</c> / <c>oracle-cards.json</c> are
/// gitignored and only present after the <c>InteractionTriage</c>/<c>MagicAstTriage</c> Flowthru flow has
/// run (main checkout). The corpus/bulk PATHS are overridable via
/// <c>MAST_DRIFT_SWEEP_CORPUS_PATH</c>/<c>MAST_DRIFT_SWEEP_BULK_PATH</c> — not for normal use, but so the
/// sweep LOGIC (the join + compare + propose, independent of which worktree happens to have the real
/// corpus checked out) can be exercised against a small synthetic authoritative-source file.
/// </para>
/// </summary>
[TestFixture]
[Explicit(
  "Maintenance utility: proposes oracle-text-quarantine.json add/remove entries from a fresh drift "
    + "sweep against the authoritative corpus. Never auto-edits the quarantine. Run on demand."
)]
public class QuarantineDriftSweepUtility
{
  private sealed record DriftProposal(
    string Fixture,
    string Card,
    string Source,
    string GoldText,
    string AuthoritativeText
  );

  private sealed record SweepProposal(
    string GeneratedAt,
    int CheckedCount,
    int AddCount,
    int RemoveCount,
    IReadOnlyList<DriftProposal> Add,
    IReadOnlyList<DriftProposal> Remove
  );

  [Test]
  public void Propose_quarantine_changes_from_drift_sweep()
  {
    var corpus = LoadCorpus(
      Environment.GetEnvironmentVariable("MAST_DRIFT_SWEEP_CORPUS_PATH") ?? TestData.CardInputsPath
    );
    var bulk = LoadBulk(
      Environment.GetEnvironmentVariable("MAST_DRIFT_SWEEP_BULK_PATH") ?? TestData.OracleCardsPath
    );

    if (corpus is null && bulk is null)
    {
      Assert.Ignore(
        "Neither card-inputs.json nor oracle-cards.json is present — the drift sweep needs at least one "
          + "authoritative source. Both are gitignored and only present after the InteractionTriage/"
          + "MagicAstTriage flow has run (main checkout)."
      );
      return;
    }

    var quarantined = LoadQuarantinedFixtures();

    var adds = new List<DriftProposal>();
    var removes = new List<DriftProposal>();
    var checkedCount = 0;

    foreach (var testCase in HandParsedTestCaseLoader.GetAllTestCases())
    {
      var name = testCase.InputNode["Name"]?.ToString();
      var goldText = testCase.InputNode["OracleText"]?.ToString();
      if (name is null)
        continue;

      // DFC/MDFC golds carry no top-level Input.OracleText — the real text lives per-face in
      // Input.CardFaces[].OracleText. Compose it the SAME way LoadBulk composes the authoritative side's
      // card_faces (blank-line join), so a two-faced card is compared face-text-to-face-text instead of
      // a null gold side against a genuinely-composed authoritative side (which reads as 100% drift for
      // every DFC regardless of correctness — found by review, 2026-07-18).
      if (string.IsNullOrWhiteSpace(goldText) && testCase.InputNode["CardFaces"] is System.Text.Json.Nodes.JsonArray faces)
      {
        goldText = string.Join(
          "\n\n",
          faces
            .Select(f => f?["OracleText"]?.ToString())
            .Where(t => !string.IsNullOrEmpty(t))
        );
      }

      // Corpus wins on conflict (it is the exact text the parser consumes); the bulk fills gaps for
      // cards filtered out of the commander-legal-paper corpus — same precedence as
      // GoldOracleTextFidelityTests / GoldRegenerationUtility.
      string? authoritative = null;
      var source = "";
      if (corpus is not null && corpus.TryGetValue(name, out var c) && c is not null)
      {
        authoritative = c;
        source = "corpus";
      }
      else if (bulk is not null && bulk.TryGetValue(name, out var b) && b is not null)
      {
        authoritative = b;
        source = "bulk";
      }

      if (authoritative is null)
        continue; // neither source has this card — cannot judge drift (synthetic-card-golds territory)

      checkedCount++;
      var isQuarantined = quarantined.Contains(testCase.Name);
      var matches = Norm(goldText) == Norm(authoritative);

      if (!matches && !isQuarantined)
        adds.Add(new DriftProposal(testCase.Name, name, source, Norm(goldText), Norm(authoritative)));
      else if (matches && isQuarantined)
        removes.Add(new DriftProposal(testCase.Name, name, source, Norm(goldText), Norm(authoritative)));
    }

    adds = [.. adds.OrderBy(a => a.Fixture, StringComparer.Ordinal)];
    removes = [.. removes.OrderBy(r => r.Fixture, StringComparer.Ordinal)];

    TestContext.WriteLine($"Checked {checkedCount} fixtures against an authoritative source.");
    TestContext.WriteLine($"  ADD (drifted, not yet quarantined): {adds.Count}");
    foreach (var a in adds.Take(25))
      TestContext.WriteLine($"    {a.Fixture} ({a.Card}, via {a.Source})");
    if (adds.Count > 25)
      TestContext.WriteLine($"    ... and {adds.Count - 25} more (see the written report)");
    TestContext.WriteLine($"  REMOVE (quarantined, now matches — the shrink signal): {removes.Count}");
    foreach (var r in removes)
      TestContext.WriteLine($"    {r.Fixture} ({r.Card}, via {r.Source})");

    var report = new SweepProposal(
      GeneratedAt: "Tests/Parse/QuarantineDriftSweepUtility",
      CheckedCount: checkedCount,
      AddCount: adds.Count,
      RemoveCount: removes.Count,
      Add: adds,
      Remove: removes
    );

    var outDir = Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "_08_Reporting");
    Directory.CreateDirectory(outDir);
    var outPath = Path.Combine(outDir, "quarantine-sweep-proposal.json");
    var options = new JsonSerializerOptions
    {
      WriteIndented = true,
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
    File.WriteAllText(outPath, JsonSerializer.Serialize(report, options) + "\n");
    TestContext.WriteLine($"Wrote {outPath}");
    TestContext.WriteLine(
      "This is a PROPOSAL only — oracle-text-quarantine.json is NOT auto-edited. Review each entry and "
        + "hand-apply (per the governing principle: a sweep may propose, but accepting/rejecting an "
        + "exemption stays a human-confirmed decision)."
    );
  }

  // Mirrors GoldOracleTextFidelityTests.LoadCorpus (Name -> OracleText, card-inputs.json shape).
  private static Dictionary<string, string?>? LoadCorpus(string path)
  {
    if (!File.Exists(path))
      return null;

    var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    foreach (var rec in doc.RootElement.EnumerateArray())
    {
      if (!rec.TryGetProperty("Input", out var input))
        continue;
      var name = input.TryGetProperty("Name", out var n) ? n.GetString() : null;
      if (name is null || dict.ContainsKey(name))
        continue;
      dict[name] = input.TryGetProperty("OracleText", out var ot) ? ot.GetString() : null;
    }
    return dict;
  }

  // Mirrors GoldOracleTextFidelityTests.LoadOracleCards (Name -> oracle_text, oracle-cards.json bulk
  // shape, with the DFC per-face join).
  private static Dictionary<string, string?>? LoadBulk(string path)
  {
    if (!File.Exists(path))
      return null;

    var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    foreach (var rec in doc.RootElement.EnumerateArray())
    {
      var name = rec.TryGetProperty("name", out var n) ? n.GetString() : null;
      if (name is null || dict.ContainsKey(name))
        continue;

      var text = rec.TryGetProperty("oracle_text", out var ot) ? ot.GetString() : null;
      if (string.IsNullOrWhiteSpace(text) && rec.TryGetProperty("card_faces", out var faces))
      {
        text = string.Join(
          "\n\n",
          faces
            .EnumerateArray()
            .Select(f => f.TryGetProperty("oracle_text", out var fot) ? fot.GetString() : null)
            .Where(t => !string.IsNullOrEmpty(t))
        );
      }
      dict[name] = text;
    }
    return dict;
  }

  private static HashSet<string> LoadQuarantinedFixtures()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "oracle-text-quarantine.json"
    );
    var set = new HashSet<string>(StringComparer.Ordinal);
    if (!File.Exists(path))
      return set;

    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    if (doc.RootElement.TryGetProperty("entries", out var entries))
      foreach (var e in entries.EnumerateArray())
        if (e.TryGetProperty("card", out var c) && c.GetString() is { } card)
          set.Add(card);
    return set;
  }

  // Trivial normalization only — identical to GoldOracleTextFidelityTests.Norm (line endings + surrounding
  // whitespace); everything else is real drift.
  private static string Norm(string? s) => s is null ? "" : s.Replace("\r\n", "\n").Trim();
}
