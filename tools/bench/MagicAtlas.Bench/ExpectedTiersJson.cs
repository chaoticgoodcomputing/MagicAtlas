using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagicAtlas.Bench;

/// <summary>
/// One pinned combo entry on <c>combo-expected-tiers.json</c> (post 2026-07-18 schema split). Two
/// fenced halves per the governing principle (code/data are the source of truth; hand-maintained state
/// that can silently drift from reality is a defect):
/// <list type="bullet">
///   <item><b>Mechanistic</b> — <see cref="ExpectedTier"/> and <see cref="Expected"/> — MECHANICALLY
///   derived from a live engine run (<see cref="ExpectedDiagnostics.FromDiagnostics"/>), never
///   hand-typed, and gate-checked every run (<see cref="ComboExpectedTierTest"/>).</item>
///   <item><b>Narrative</b> — <see cref="Narrative"/> + <see cref="NarrativeVerifiedAt"/> — CR
///   citations, judge history, design rationale. Genuinely needs a human; hand-authored and NOT
///   machine-verified. Carried over verbatim from the pre-migration <c>reason</c> field.</item>
/// </list>
/// </summary>
public sealed record ExpectedTierPin
{
  [JsonPropertyName("id")]
  public required string Id { get; init; }

  [JsonPropertyName("cards")]
  public required IReadOnlyList<string> Cards { get; init; }

  [JsonPropertyName("expectedTier")]
  public required string ExpectedTier { get; init; }

  /// <summary>
  /// The mechanically-derived §8 verdict for this combo's winning cycle. <c>null</c> ONLY for
  /// <c>Missed</c> combos (no cycle exists to derive diagnostics from) — never null/omitted for
  /// Green or Amber.
  /// </summary>
  [JsonPropertyName("expected")]
  public ExpectedDiagnostics? Expected { get; init; }

  /// <summary>Hand-authored narrative — CR citations, judge history, design rationale. NOT machine-checked.</summary>
  [JsonPropertyName("narrative")]
  public required string Narrative { get; init; }

  /// <summary>The date (yyyy-MM-dd) a human last verified <see cref="Narrative"/> still describes reality.</summary>
  [JsonPropertyName("narrativeVerifiedAt")]
  public required string NarrativeVerifiedAt { get; init; }
}

/// <summary>The whole <c>combo-expected-tiers.json</c> document.</summary>
public sealed record ExpectedTiersDocument
{
  [JsonPropertyName("_doc")]
  public required string Doc { get; init; }

  [JsonPropertyName("combos")]
  public required IReadOnlyList<ExpectedTierPin> Combos { get; init; }
}

/// <summary>Deterministic (de)serialization of <see cref="ExpectedTiersDocument"/>, mirroring <c>BenchReportJson</c>.</summary>
public static class ExpectedTiersJson
{
  private static readonly JsonSerializerOptions Options = new()
  {
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    IndentCharacter = ' ',
    IndentSize = 2,
  };

  public static string Serialize(ExpectedTiersDocument doc) =>
    JsonSerializer.Serialize(doc, Options) + "\n";

  public static void Write(string path, ExpectedTiersDocument doc) =>
    File.WriteAllText(path, Serialize(doc));

  public static ExpectedTiersDocument Read(string path) =>
    JsonSerializer.Deserialize<ExpectedTiersDocument>(File.ReadAllText(path), Options)
    ?? throw new InvalidOperationException($"Could not parse expected-tiers document at {path}");
}
