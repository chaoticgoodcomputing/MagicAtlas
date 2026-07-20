namespace MagicAST.Tests.Tests.CardAtlas;

using System.Text.Json;
using MagicAST.Tests.Infrastructure;

/// <summary>
/// <c>Fixtures/CardAtlas/sac-fixture.json</c> is the tiny committed slice the
/// <see cref="CardAtlasContractTests"/> gate runs the D1–D4 pipeline over. Two different things live in
/// that one file, and they have opposite rules:
///
/// <list type="bullet">
///   <item><b>The selection is Evidence.</b> A human chose Chatterfang × Pitiless Plunderer plus a sac
///     outlet and a death payoff because that set exercises the contract. Nothing derives that choice and
///     nothing should — it is a judgment about what is worth testing.</item>
///   <item><b>The content is not.</b> Each card's oracle text must equal what Scryfall prints. It is
///     copied, not authored, so a human editing it is a defect and a human re-typing it is a risk.</item>
/// </list>
///
/// <para>This test gates the second half. Without it the fixture is a hand-maintained copy of external
/// source data — the exact shape ADR-0004 §1 calls a derived claim — and its drift is invisible, because
/// the pipeline happily parses stale text into a self-consistent (and wrong) answer.</para>
///
/// <para><b>Not hypothetical.</b> Commit <c>295f3506</c> was "correct stale Suture Priest oracle text" —
/// a gold whose hand-copied text had fallen behind an oracle update, which moved a combo's reconstruction
/// tier. Same failure mode, a different file. The parse-track golds gained a fidelity gate afterwards
/// (<see cref="MagicAST.Tests.Tests.GoldOracleTextFidelityTests"/>); this fixture never did.</para>
/// </summary>
[TestFixture]
public class SacFixtureOracleFidelityTests
{
  private sealed record FixtureCard(string Name, string OracleText);

  private sealed record Fixture(List<FixtureCard> Cards);

  private static List<FixtureCard> FixtureCards()
  {
    var path = Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Fixtures",
      "CardAtlas",
      "sac-fixture.json"
    );
    return JsonSerializer.Deserialize<Fixture>(
      File.ReadAllText(path),
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
    )!.Cards;
  }

  public static IEnumerable<TestCaseData> Cards() =>
    FixtureCards().Select(c => new TestCaseData(c.Name, c.OracleText).SetName($"{{m}}({c.Name})"));

  /// <summary>
  /// THE GATE. Byte-exact against the Scryfall bulk. Skips loudly when the bulk is absent (it is
  /// gitignored, so a fresh checkout has none) rather than passing vacuously — the distinction this whole
  /// initiative turns on.
  /// </summary>
  [TestCaseSource(nameof(Cards))]
  public void Fixture_oracle_text_matches_scryfall(string name, string fixtureText)
  {
    var bulk = TestData.LoadOracleCardsByName();
    if (bulk is null)
      Assert.Ignore(
        $"Scryfall bulk absent at {TestData.OracleCardsPath} — cannot validate '{name}'. "
          + "Regenerate with `dotnet run -- --flow MagicAstTriage`."
      );

    Assert.That(
      bulk!.TryGetValue(name, out var printed),
      Is.True,
      $"'{name}' is not in the Scryfall bulk at all — the fixture names a card that does not exist "
        + "(a typo, or a name that changed upstream)."
    );

    Assert.That(
      fixtureText.Trim().ReplaceLineEndings("\n"),
      Is.EqualTo((printed ?? string.Empty).Trim().ReplaceLineEndings("\n")),
      $"sac-fixture.json's oracle text for '{name}' has drifted from Scryfall. The fixture's CARD SELECTION "
        + "is a human judgment and stays hand-authored; its oracle TEXT is copied from the source and must "
        + "match. Update the fixture from the bulk — do not edit the expectation to match the fixture."
    );
  }

  /// <summary>
  /// Non-vacuity: an empty or unreadable fixture would make the case source produce nothing and the gate
  /// above would "pass" by never running.
  /// </summary>
  [Test]
  public void The_fixture_has_cards_to_check() =>
    Assert.That(FixtureCards(), Is.Not.Empty, "sac-fixture.json declared no cards — the gate is vacuous");
}
