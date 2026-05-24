using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Source step: fetches Scryfall's oracle-cards bulk dump and projects it into
/// <see cref="MastRawScryfallCard"/>. Two-stage fetch — metadata endpoint
/// returns the rotating <c>download_uri</c>, then we stream that.
/// </summary>
/// <remarks>
/// Flowthru's smart caching reuses the cached output file (<c>oracle-cards.json</c>
/// on disk) across runs — this step only re-executes when the file is missing
/// or the step's fingerprint changes.
/// </remarks>
[FlowthruStep]
public static class FetchScryfallBulkStep
{
  private const string MetadataUrl = "https://api.scryfall.com/bulk-data/oracle-cards";

  private static readonly JsonSerializerOptions s_jsonOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  public static Func<Task<IEnumerable<MastRawScryfallCard>>> Create(HttpClient httpClient) =>
    async () =>
    {
      var downloadUri = await ResolveDownloadUri(httpClient);
      await using var stream = await httpClient.GetStreamAsync(downloadUri);
      var cards = await JsonSerializer.DeserializeAsync<List<MastRawScryfallCard>>(
        stream,
        s_jsonOptions
      );
      return cards ?? new List<MastRawScryfallCard>();
    };

  private static async Task<string> ResolveDownloadUri(HttpClient httpClient)
  {
    var json = await httpClient.GetStringAsync(MetadataUrl);
    using var doc = JsonDocument.Parse(json);
    if (
      !doc.RootElement.TryGetProperty("download_uri", out var element)
      || element.ValueKind != JsonValueKind.String
      || string.IsNullOrWhiteSpace(element.GetString())
    )
    {
      throw new InvalidOperationException(
        $"Scryfall bulk-data metadata at '{MetadataUrl}' returned no download_uri. "
          + $"Response head: {json[..Math.Min(200, json.Length)]}"
      );
    }
    return element.GetString()!;
  }
}
