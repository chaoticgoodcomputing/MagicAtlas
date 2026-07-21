using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using MagicAST.Interaction;

namespace MagicAtlas.Bench;

/// <summary>
/// One row of the eligible-set ROSTER — a combo id and the cards it is reconstructed from. <b>Derived,
/// not Evidence</b> (ADR 0004 §1): both fields are properties of the pinned Commander Spellbook snapshot
/// crossed with the gold corpus, and <c>ComboAxisExpectationTest</c> asserts the roster equals the live
/// eligible set exactly. It is committed (rather than computed at read time) for one reason: two
/// CORE-ring consumers — the ADR-0004 §4 quarantine→tier cross-track join and the fidelity blast-radius
/// report — need <c>card → combo</c> without being able to run the interaction engine. Regenerate with
/// <c>dotnet run -- --regenerate-roster</c>; it carries NO verdict and NO engine output.
/// </summary>
public sealed record ComboRosterEntry
{
  [JsonPropertyName("id")]
  public required string Id { get; init; }

  [JsonPropertyName("cards")]
  public required IReadOnlyList<string> Cards { get; init; }
}

/// <summary>
/// One AXIS EXCEPTION — the Evidence half (ADR 0004 §1/§5). Reads: <i>combo <see cref="Combo"/> is
/// expected NOT to satisfy axis <see cref="Axis"/>, and a human has ruled that this is
/// <see cref="Verdict"/>.</i>
///
/// <para><b>Existence is derived; acceptance is judged.</b> The engine computes <i>that</i> the axis
/// fails — so the gate's failure message prints the exact entry to paste, and no tool ever invents an
/// exception. What no tool may ever write is <see cref="Verdict"/>: regenerating it from the engine's own
/// output would make the gate assert that the engine agrees with itself, which can never fail (ADR 0004
/// §5.2). A freshly-pasted entry carries <see cref="Verdicts.Unjudged"/>, which is a HARD FAILURE until a
/// human or the interaction-judge rules on it.</para>
///
/// <para><b>Honest classification:</b> this is per-combo state, so it is a NARROWER PIN, not a stateless
/// invariant. What IS stateless is the default it sits against — every eligible combo with no exception
/// entry is expected to satisfy all five axes, so a newly-eligible combo needs no edit to be covered, and
/// a silently-degrading one has nowhere to hide.</para>
/// </summary>
public sealed record ComboAxisException
{
  [JsonPropertyName("combo")]
  public required string Combo { get; init; }

  /// <summary>One of <see cref="ComboPlainLanguage.Axes"/>.</summary>
  [JsonPropertyName("axis")]
  public required string Axis { get; init; }

  /// <summary>One of <see cref="Verdicts"/>. HAND-SET ONLY — never written by any tool.</summary>
  [JsonPropertyName("verdict")]
  public required string Verdict { get; init; }

  /// <summary>Non-authoritative human narrative (ADR 0004 §5.3). No gate and no report treats this as
  /// truth; it may be empty. Nothing derives from it and nothing checks it.</summary>
  [JsonPropertyName("note")]
  public string Note { get; init; } = "";
}

/// <summary>
/// One combo that produces NO reconstruction at all (the old <c>Missed</c> tier). Not an axis
/// expectation — there is no cycle to have axes. Kept here deliberately; see the file's <c>_doc</c>.
/// </summary>
public sealed record UnreconstructedCombo
{
  [JsonPropertyName("combo")]
  public required string Combo { get; init; }

  [JsonPropertyName("verdict")]
  public required string Verdict { get; init; }

  [JsonPropertyName("note")]
  public string Note { get; init; } = "";
}

/// <summary>The legal <c>verdict</c> values. Only a human (or the interaction-judge) sets these.</summary>
public static class Verdicts
{
  /// <summary>The axis genuinely does not hold in Magic terms — the engine is RIGHT to floor it.</summary>
  public const string Genuine = "genuine";

  /// <summary>The axis fails because the model is coarse, not because Magic says so — known debt, still
  /// pinned so it cannot move unnoticed, but flagged as something to fix rather than to celebrate.</summary>
  public const string ModellingGap = "modelling-gap";

  /// <summary>Inherited verbatim from the pre-#31 pin file, whose prose carried NO judge attestation.
  /// DEBT, not an endorsement: the honest statement that this expectation was never independently ruled
  /// on. A visible burn-down list for the interaction-judge, not a way to avoid one.</summary>
  public const string CarriedOver = "carried-over";

  /// <summary>No cycle is reconstructed for this combo at all (unreconstructed section only).</summary>
  public const string NoReconstruction = "no-reconstruction";

  /// <summary>The placeholder the gate's failure message emits. ALWAYS a hard failure.</summary>
  public const string Unjudged = "UNJUDGED";

  public static readonly IReadOnlySet<string> AxisVerdicts = new HashSet<string>(StringComparer.Ordinal)
  {
    Genuine,
    ModellingGap,
    CarriedOver,
  };
}

/// <summary>The whole <c>combo-axis-expectations.json</c> document.</summary>
public sealed record ComboAxisExpectationsDocument
{
  [JsonPropertyName("_doc")]
  public required string Doc { get; init; }

  [JsonPropertyName("axes")]
  public required IReadOnlyList<string> Axes { get; init; }

  [JsonPropertyName("combos")]
  public required IReadOnlyList<ComboRosterEntry> Combos { get; init; }

  [JsonPropertyName("axisExceptions")]
  public required IReadOnlyList<ComboAxisException> AxisExceptions { get; init; }

  [JsonPropertyName("unreconstructed")]
  public required IReadOnlyList<UnreconstructedCombo> Unreconstructed { get; init; }

  /// <summary>combo id → the axes it is expected to FAIL (from <see cref="AxisExceptions"/>).</summary>
  public IReadOnlyDictionary<string, IReadOnlyList<string>> ExpectedFailingAxesByCombo =>
    AxisExceptions
      .GroupBy(e => e.Combo, StringComparer.Ordinal)
      .ToDictionary(
        g => g.Key,
        g =>
          (IReadOnlyList<string>)
            [
              .. g.Select(e => e.Axis)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(a => ComboPlainLanguage.Axes.ToList().IndexOf(a)),
            ],
        StringComparer.Ordinal
      );
}

/// <summary>Deterministic (de)serialization, mirroring <c>BenchReportJson</c>.</summary>
public static class ComboAxisExpectationsJson
{
  private static readonly JsonSerializerOptions Options = new()
  {
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    IndentCharacter = ' ',
    IndentSize = 2,
  };

  public static string Serialize(ComboAxisExpectationsDocument doc) =>
    JsonSerializer.Serialize(doc, Options) + "\n";

  public static void Write(string path, ComboAxisExpectationsDocument doc) =>
    File.WriteAllText(path, Serialize(doc));

  public static ComboAxisExpectationsDocument Read(string path) =>
    JsonSerializer.Deserialize<ComboAxisExpectationsDocument>(File.ReadAllText(path), Options)
    ?? throw new InvalidOperationException($"Could not parse combo-axis-expectations at {path}");
}

/// <summary>
/// Regenerates ONLY the <c>combos</c> roster (id + cards) from a live run. It rewrites nothing else:
/// <c>axisExceptions</c> and <c>unreconstructed</c> are carried over verbatim, because their
/// <c>verdict</c> field is judge-set Evidence and a tool that could write it would make the gate vacuous
/// (ADR 0004 §5.2). There is deliberately NO <c>--regenerate-expectations</c>.
/// </summary>
public static class ComboRosterRegeneration
{
  public static void Regenerate(string path, ComboRecallRunner runner, ComboSnapshot snapshot)
  {
    var current = runner.Run(snapshot);
    var doc = ComboAxisExpectationsJson.Read(path);

    ComboAxisExpectationsJson.Write(
      path,
      doc with
      {
        Axes = ComboPlainLanguage.Axes,
        Combos =
        [
          .. current
            .Combos.OrderBy(c => c.Id, StringComparer.Ordinal)
            .Select(c => new ComboRosterEntry
            {
              Id = c.Id,
              Cards = [.. c.Cards.Distinct(StringComparer.Ordinal)],
            }),
        ],
      }
    );
  }
}
