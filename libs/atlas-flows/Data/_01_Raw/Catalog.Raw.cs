using Flowthru.Data.Catalog;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Scryfall card-symbology payload. Populated by <c>FetchCardSymbolsNode</c> in the
  /// <c>Ingest</c> flow, which GETs <c>https://api.scryfall.com/symbology</c> directly.
  /// </summary>
  public IItem<RawScryfallCardSymbolList> RawCardSymbols =>
    CreateItem(() =>
      Item.Of<RawScryfallCardSymbolList>("RawCardSymbols")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/symbology.json")
        .Build()
    );

  /// <summary>
  /// Scryfall oracle-cards bulk (~165 MB, ~35K cards). Populated by
  /// <c>FetchOracleCardsBulkNode</c> in the <c>Ingest</c> flow, which resolves Scryfall's
  /// daily-rotating <c>download_uri</c> via the metadata endpoint at
  /// <c>https://api.scryfall.com/bulk-data/oracle-cards</c> and downloads the body in one pass.
  /// </summary>
  public IItem<IEnumerable<RawScryfallCard>> RawCards =>
    CreateItem(() =>
      Item.Of<IEnumerable<RawScryfallCard>>("RawCards")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/oracle-cards.json")
        .Build()
    );
}
