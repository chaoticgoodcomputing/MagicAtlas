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

  // ── Corpus-parse inputs — promoted from tests/magic-ast-tests so this library can regenerate the
  //    CardAtlas file-drop inputs (card-inputs.json / parse-records.json / combos.json). ────────────

  /// <summary>
  /// Scryfall oracle-cards bulk projected to the NARROW <see cref="MastRawScryfallCard"/> — fetched by
  /// <c>FetchScryfallBulkStep</c> in the <c>CorpusParse</c> flow. Kept on a distinct filename from
  /// <see cref="RawCards"/> (which holds the richer <c>RawScryfallCard</c> Ingest projection) so the two
  /// fetches never clobber each other's on-disk cache. Consumed by <c>ProjectToCardInputStep</c>.
  /// </summary>
  public IItem<IEnumerable<MastRawScryfallCard>> RawScryfallCards =>
    CreateItem(() =>
      Item.Of<IEnumerable<MastRawScryfallCard>>("RawScryfallCards")
        .Json()
        .AtPath($"{_basePath}/_01_Raw/Datasets/External/mast-oracle-cards.json")
        .Build()
    );

  /// <summary>
  /// Commander Spellbook's combo dump (<c>variants.json</c>, ~510 MB) as a plain HTTP catalog item — the
  /// <c>.Json()</c> singleton builder routes the <c>https://</c> URI through <c>UseHttp</c>'s
  /// <c>HttpStorageMedium</c>. <c>FetchCombosStep</c> reads this and projects it to the lean
  /// <see cref="MagicAtlas.Data._02_Intermediate.Schemas.Combo"/> work-list; the host's <c>UseHttp</c>
  /// conditional-GET cache (under <c>_01_Raw/Datasets/External/.http-cache</c>) means a fresh clone
  /// fetches the dump once and reuses it — no manual curl. The narrow <see cref="CsbVariantsDump"/> schema
  /// drops CSB's bloat (image URIs, prices, card-state) on read; the raw bytes are never committed.
  /// </summary>
  public IItem<CsbVariantsDump> CsbVariantsRaw =>
    CreateItem(() =>
      Item.Of<CsbVariantsDump>("CsbVariantsRaw")
        .Json()
        .AtPath("https://json.commanderspellbook.com/variants.json")
        .Build()
    );
}
