using System.Text.Json;
using Flowthru.Step;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Flows.Ingest.Nodes;

/// <summary>
/// Source step that fetches Scryfall's oracle-cards bulk JSON (~165 MB, ~35K cards) and returns
/// it as <c>IEnumerable&lt;RawScryfallCard&gt;</c>. The fetch is a two-stage process: GET the
/// stable bulk-data metadata endpoint to discover the current <c>download_uri</c>, then GET the
/// daily-rotating bulk file at that URL.
/// </summary>
/// <remarks>
/// Self-contained — the metadata indirection is wrapped inside this single step rather than
/// surfacing it as a separate catalog item. Each Ingest run re-fetches the current bulk; we don't
/// hold a conditional-GET cache here (Flowthru's <c>UseHttp</c> cache covers HTTP-backed catalog
/// items, not in-step <see cref="HttpClient"/> calls — adding caching here would mean either
/// reaching through <see cref="IStorageMediumResolver"/> or rolling our own keyed file cache,
/// neither warranted while the typical use is "run Ingest once per dev session").
/// <para>
/// System.Text.Json doesn't honor Flowthru's <c>[SerializedLabel]</c> attribute, so we rely on
/// <see cref="JsonNamingPolicy.SnakeCaseLower"/> to convert PascalCase / underscored C# property
/// names to the snake_case keys in Scryfall's response. Flowthru's <c>JsonFormatSerializer</c>
/// on the write side does honor <c>[SerializedLabel]</c>, so the on-disk file ends up consistent
/// with the rest of the pipeline's serialization conventions.
/// </para>
/// </remarks>
[FlowthruStep]
public static class FetchOracleCardsBulkNode
{
  private const string MetadataUrl = "https://api.scryfall.com/bulk-data/oracle-cards";

  private static readonly JsonSerializerOptions s_jsonOptions =
    new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

  public static Func<Task<IEnumerable<RawScryfallCard>>> Create(HttpClient httpClient)
  {
    return async () =>
    {
      var downloadUri = await ResolveDownloadUri(httpClient);
      await using var stream = await httpClient.GetStreamAsync(downloadUri);
      var cards = await JsonSerializer.DeserializeAsync<List<RawScryfallCard>>(
        stream,
        s_jsonOptions
      );
      return cards ?? new List<RawScryfallCard>();
    };
  }

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
          + $"Response head: {json.Substring(0, Math.Min(200, json.Length))}"
      );
    }
    return element.GetString()!;
  }
}
