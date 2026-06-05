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
/// </summary>
[TestFixture]
public class GoldOracleTextFidelityTests
{
  // Name -> OracleText from the corpus the parser consumes; null when card-inputs.json is absent.
  private static readonly Lazy<Dictionary<string, string?>?> _corpus = new(LoadCorpus);
  private static readonly Lazy<HashSet<string>> _quarantine = new(LoadQuarantine);

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

  private static HashSet<string> LoadQuarantine()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "oracle-text-quarantine.json"
    );
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
      Assert.Ignore(
        $"'{name}' is not in the corpus (card-inputs.json) — cannot validate its oracle text."
      );
      return;
    }

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
