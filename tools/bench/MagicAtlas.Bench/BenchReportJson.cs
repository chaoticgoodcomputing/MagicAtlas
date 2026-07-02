using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagicAtlas.Bench;

/// <summary>
/// Deterministic (de)serialization of <see cref="BenchReport"/>. The combo-recall report must be
/// byte-identical across two runs (initiative 04 completion criterion) — so the writer fixes the
/// indentation, the property order (via member order), the enum rendering (string), and a trailing LF,
/// and the runner already sorts combos by id. No timestamps, no machine paths.
/// </summary>
public static class BenchReportJson
{
  private static readonly JsonSerializerOptions Options = new()
  {
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    // Keep apostrophes/ampersands in card names literal (Narset's Reversal, Ashnod's Altar) — the
    // report is committed and read by humans; the relaxed encoder is still deterministic.
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    // The newer .NET indenter defaults to 2 spaces; pin it explicitly for stability across SDKs.
    IndentCharacter = ' ',
    IndentSize = 2,
  };

  public static string Serialize(BenchReport report) =>
    JsonSerializer.Serialize(report, Options) + "\n";

  public static void Write(string path, BenchReport report) =>
    File.WriteAllText(path, Serialize(report));

  public static BenchReport Read(string path) =>
    JsonSerializer.Deserialize<BenchReport>(File.ReadAllText(path), Options)
    ?? throw new InvalidOperationException($"Could not parse bench report at {path}");
}
