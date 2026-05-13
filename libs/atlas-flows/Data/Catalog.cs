using System.Text.Json;
using Flowthru.Data.Catalog;
using Flowthru.Data.Storage;

namespace MagicAtlas.Data;

/// <summary>
/// Local-filesystem + HTTP data catalog for the atlas pipelines.
/// Raw resources (Scryfall bulk data, MTG comprehensive rules text) are fetched at flow-run time
/// through Flowthru's HTTP storage medium with conditional-GET caching; the harness supplies the
/// <see cref="IStorageMediumResolver"/> wired in by <c>UseHttp()</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two of the upstream raw URLs rotate and have to be resolved at runtime:
/// </para>
/// <list type="bullet">
/// <item><b>Scryfall oracle-cards bulk</b> — the metadata at
/// <c>https://api.scryfall.com/bulk-data/oracle-cards</c> publishes a daily-rotating
/// <c>download_uri</c>. Resolved lazily via <see cref="_oracleCardsBulkUrl"/> so that
/// <c>--list</c> and other no-flow invocations don't trigger network IO.</item>
/// <item><b>MTG comprehensive rules</b> — Wizards' rules page links to a dated text file (e.g.
/// <c>MagicCompRules%2020260417.txt</c>) that changes a few times a year. The
/// <c>FetchRulesTextNode</c> step scrapes the index page at flow-run time.</item>
/// </list>
/// </remarks>
public partial class Catalog : CatalogAbstract
{
  /// <summary>Stable URL for Scryfall's oracle-cards bulk-data metadata endpoint.</summary>
  private const string ScryfallOracleCardsMetadataUrl =
    "https://api.scryfall.com/bulk-data/oracle-cards";

  private readonly string _basePath;
  private readonly HttpClient _httpClient;
  private readonly IStorageMediumResolver? _resolver;

  /// <summary>
  /// Today's Scryfall oracle-cards <c>download_uri</c>. Resolved synchronously on first access
  /// via a small (~5 KB) GET against the metadata endpoint, then memoised for the process.
  /// </summary>
  private readonly Lazy<string> _oracleCardsBulkUrl;

  public Catalog(
    string basePath,
    HttpClient httpClient,
    IStorageMediumResolver? resolver = null
  )
  {
    _basePath = basePath;
    _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    _resolver = resolver;
    _oracleCardsBulkUrl = new Lazy<string>(ResolveOracleCardsBulkUrl);
  }

  private string ResolveOracleCardsBulkUrl()
  {
    // System.Text.Json doesn't understand Flowthru's [SerializedLabel("download_uri")] mapping,
    // so we read the field directly out of a JsonDocument instead of binding to the strongly
    // typed `ScryfallBulkMetadata` schema — we only need one field, after all.
    var json = _httpClient
      .GetStringAsync(ScryfallOracleCardsMetadataUrl)
      .GetAwaiter()
      .GetResult();
    using var doc = JsonDocument.Parse(json);
    if (
      !doc.RootElement.TryGetProperty("download_uri", out var downloadUri)
      || downloadUri.ValueKind != JsonValueKind.String
      || string.IsNullOrWhiteSpace(downloadUri.GetString())
    )
    {
      throw new InvalidOperationException(
        $"Scryfall bulk-data metadata at '{ScryfallOracleCardsMetadataUrl}' returned no download_uri. "
          + $"Raw response head: {json.Substring(0, Math.Min(200, json.Length))}"
      );
    }
    return downloadUri.GetString()!;
  }
}
