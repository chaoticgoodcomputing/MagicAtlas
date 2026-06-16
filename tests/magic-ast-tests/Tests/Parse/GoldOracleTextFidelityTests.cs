namespace MagicAST.Tests.Tests;

using System.Text.Json;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// Fidelity smoke test: a gold fixture's <c>Input.OracleText</c> must be the exact oracle text the
/// corpus parser consumes for that card (<c>card-inputs.json</c>, a faithful projection of Scryfall).
///
/// <para>
/// The per-card parser tests (<see cref="HandParsedCardTests"/>) cannot catch a wrong input string:
/// the expected AST and its <c>RawText</c> spans are sliced from the same <c>Input.OracleText</c>, so
/// a gold authored from the wrong card is internally self-consistent and parses green. This test is
/// the only thing that reaches an authoritative source, so it is the only thing that catches a gold
/// drifting from (or never matching) the real card.
/// </para>
///
/// <para>
/// Cards on <c>Fixtures/oracle-text-quarantine.json</c> are known pre-existing drift, exempt from the
/// match assertion. The quarantine is a ratchet that only shrinks: a quarantined gold that is fixed
/// (now matches) fails until removed from the list, and any gold NOT on the list must match — so no
/// new drift can land. See <c>libs/mast-interaction/docs/interaction-refinement-round2.md</c>.
/// </para>
///
/// <para>
/// A gold whose card is ABSENT from the corpus cannot be validated against an authoritative source at
/// all — the dangerous case, because such a gold parses green on pure self-consistency (wrong oracle
/// text slips through). That hole is closed the same way as drift: golds on
/// <c>Fixtures/fidelity-uncovered-allowlist.json</c> are the grandfathered current absentees, and any
/// gold absent from the corpus that is NOT on the allowlist is a LOUD failure. The allowlist is a
/// ratchet that only shrinks: once a gold's card appears in the corpus it must be removed (it now gets
/// real validation), and no new absentee can land un-justified. Prefer making the card present in the
/// corpus (so it gets real validation) over adding it to the allowlist.
/// </para>
/// </summary>
[TestFixture]
public class GoldOracleTextFidelityTests
{
  // Name -> OracleText from the corpus the parser consumes; null when card-inputs.json is absent.
  private static readonly Lazy<Dictionary<string, string?>?> _corpus = new(LoadCorpus);
  private static readonly Lazy<HashSet<string>> _quarantine = new(
    () => LoadFixtureNameSet("oracle-text-quarantine.json")
  );

  // testCase.Name set of golds whose card is currently absent from the corpus and so cannot be
  // validated — grandfathered, shrink-only (see class doc).
  private static readonly Lazy<HashSet<string>> _uncoveredAllowlist = new(
    () => LoadFixtureNameSet("fidelity-uncovered-allowlist.json")
  );

  private static Dictionary<string, string?>? LoadCorpus()
  {
    var path = TestData.CardInputsPath;
    if (!File.Exists(path))
    {
      return null;
    }

    var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
    using var doc = JsonDocument.Parse(File.ReadAllText(path));
    foreach (var rec in doc.RootElement.EnumerateArray())
    {
      if (!rec.TryGetProperty("Input", out var input))
      {
        continue;
      }

      var name = input.TryGetProperty("Name", out var n) ? n.GetString() : null;
      if (name is null || dict.ContainsKey(name))
      {
        continue;
      }

      dict[name] = input.TryGetProperty("OracleText", out var ot) ? ot.GetString() : null;
    }

    return dict;
  }

  // Loads a committed Fixtures/*.json allowlist (a flat JSON array of testCase.Name strings).
  private static HashSet<string> LoadFixtureNameSet(string fileName)
  {
    var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fileName);
    if (!File.Exists(path))
    {
      return new HashSet<string>(StringComparer.Ordinal);
    }

    var list = JsonSerializer.Deserialize<string[]>(File.ReadAllText(path)) ?? [];
    return new HashSet<string>(list, StringComparer.Ordinal);
  }

  // Trivial normalization only: line endings + surrounding whitespace. Everything else (reminder
  // text, templating drift, reordering) is a real mismatch the smoke test is meant to surface.
  private static string Norm(string? s) => s is null ? "" : s.Replace("\r\n", "\n").Trim();

  [TestCaseSource(
    typeof(HandParsedTestCaseLoader),
    nameof(HandParsedTestCaseLoader.GetTestCaseData)
  )]
  public void Gold_oracle_text_matches_corpus(CardTestCase testCase)
  {
    var corpus = _corpus.Value;
    if (corpus is null)
    {
      Assert.Ignore(
        "card-inputs.json absent — run the InteractionTriage flow to enable oracle-text fidelity checks."
      );
      return;
    }

    var name = testCase.InputNode["Name"]?.ToString();
    var goldText = testCase.InputNode["OracleText"]?.ToString();

    if (name is null)
    {
      Assert.Ignore($"Gold '{testCase.Name}' has no Input.Name — cannot validate.");
      return;
    }

    if (!corpus.TryGetValue(name, out var corpusText))
    {
      // The card is absent from the corpus, so this gold cannot be validated against an authoritative
      // source. That is exactly the dangerous case (wrong oracle text would slip through), so it is a
      // LOUD failure — UNLESS the gold is on the grandfathered, shrink-only uncovered allowlist.
      Assert.That(
        _uncoveredAllowlist.Value.Contains(testCase.Name),
        Is.True,
        $"Gold '{testCase.Name}' card '{name}' is absent from the corpus (card-inputs.json), so its "
          + "oracle text CANNOT be validated against an authoritative source — a wrong gold would slip "
          + "through. Make the card present in the corpus (preferred — re-run the InteractionTriage "
          + "flow / widen its card set so it gets real validation), or, only if truly unavoidable, add "
          + "'"
          + testCase.Name
          + "' to Fixtures/fidelity-uncovered-allowlist.json (the allowlist only shrinks)."
      );
      return;
    }

    // The card IS in the corpus now. If it is still on the uncovered allowlist, it gets real validation
    // and must be removed — the allowlist is a ratchet that only shrinks.
    Assert.That(
      _uncoveredAllowlist.Value.Contains(testCase.Name),
      Is.False,
      $"'{testCase.Name}' is on the fidelity uncovered allowlist but its card '{name}' is now in the "
        + "corpus and can be validated. Remove it from Fixtures/fidelity-uncovered-allowlist.json — the "
        + "allowlist only shrinks."
    );

    var matches = Norm(goldText) == Norm(corpusText);
    var quarantined = _quarantine.Value.Contains(testCase.Name);

    if (quarantined)
    {
      Assert.That(
        matches,
        Is.False,
        $"'{testCase.Name}' is on the oracle-text quarantine but its gold oracle text now MATCHES the "
          + "corpus. Remove it from Fixtures/oracle-text-quarantine.json — the quarantine only shrinks."
      );
      return;
    }

    Assert.That(
      matches,
      Is.True,
      $"Gold oracle text for '{testCase.Name}' differs from the corpus (card-inputs.json).\n"
        + $"  gold:   {Norm(goldText)}\n"
        + $"  corpus: {Norm(corpusText)}\n"
        + "Fix the gold's Input.OracleText to match the real card. If this is intentional known drift, "
        + "add it to Fixtures/oracle-text-quarantine.json (but prefer fixing the gold)."
    );
  }
}
