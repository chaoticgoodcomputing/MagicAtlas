using Flowthru.Data.Catalog;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw MTG comprehensive rules text. Populated by <c>FetchRulesTextNode</c>, which scrapes the
  /// rules index at <c>https://magic.wizards.com/en/rules</c> for the current dated .txt URL and
  /// follows it. Memory-only — the .txt is ~1 MB so we don't need on-disk persistence; the
  /// upstream HTML index is small enough that a fresh GET each run is cheap.
  /// </summary>
  public IItem<string> RawRules =>
    CreateItem(() => Item.Of<string>("RawRules").Memory().Build());

  /// <summary>
  /// Raw Scryfall card-symbology JSON. Populated by <c>FetchCardSymbolsNode</c>, which GETs
  /// <c>https://api.scryfall.com/symbology</c> directly. Memory-only — the payload is &lt;100 KB
  /// and Scryfall's symbology rarely changes, so a per-run fetch is fine.
  /// </summary>
  /// <remarks>
  /// Source URL: <c>https://api.scryfall.com/symbology</c>. Returns a single
  /// <c>{ object: "list", data: [...] }</c> envelope; declared as a singleton-JSON schema rather
  /// than an HTTP-backed JSON-array item because <see cref="JsonSingletonBuilder{T}"/> doesn't
  /// (yet) compose with the HTTP storage-medium resolver in Flowthru 0.17.x.
  /// </remarks>
  public IItem<RawScryfallCardSymbolList> RawCardSymbols =>
    CreateItem(() => Item.Of<RawScryfallCardSymbolList>("RawCardSymbols").Memory().Build());

  /// <summary>
  /// Raw Scryfall oracle-cards bulk JSON (~165 MB). Fetched over HTTP through Flowthru's
  /// HTTP-cached storage medium; the URL itself is resolved lazily via Scryfall's bulk-data
  /// metadata endpoint at <c>https://api.scryfall.com/bulk-data/oracle-cards</c>, which exposes a
  /// daily-rotating <c>download_uri</c>.
  /// </summary>
  /// <remarks>
  /// Conditional-GET caching against <c>data.scryfall.io</c> means same-day re-runs see a fast
  /// <c>304 Not Modified</c>; when a new bulk drops, the URL rotates and a fresh download is
  /// triggered.
  /// </remarks>
  public IItem<IEnumerable<RawScryfallCard>> RawCards =>
    CreateItem(() =>
    {
      var b = Item.Of<IEnumerable<RawScryfallCard>>("RawCards")
        .Json()
        .AtPath(_oracleCardsBulkUrl.Value);
      if (_resolver is not null)
        b = b.WithResolver(_resolver);
      return b.Build();
    });
}
