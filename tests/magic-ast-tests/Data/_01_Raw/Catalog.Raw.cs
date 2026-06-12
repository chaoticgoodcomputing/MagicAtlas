using Flowthru.Data.Catalog;
using MagicAtlas.Ast.Tests.Data._01_Raw.Schemas;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>Raw data layer: immutable source data. Currently the Scryfall oracle-cards bulk dump.</summary>
public partial class Catalog
{
  /// <summary>
  /// Scryfall oracle-cards bulk (~165 MB, ~35K cards) — fetched by
  /// <c>FetchScryfallBulkStep</c>. File-backed at the path below so Flowthru's
  /// smart caching reuses the cached download across runs.
  /// </summary>
  public IItem<IEnumerable<MastRawScryfallCard>> RawScryfallCards =>
    CreateItem(() => Item.Of<IEnumerable<MastRawScryfallCard>>("RawScryfallCards")
      .Json()
      .AtPath($"{_basePath}/_01_Raw/Datasets/External/oracle-cards.json")
      .Build());

  /// <summary>
  /// Commander Spellbook's combo dump (<c>variants.json</c>, ~510 MB) as a plain HTTP catalog item —
  /// the <c>.Json()</c> singleton builder routes the <c>https://</c> URI through <c>UseHttp</c>'s
  /// <c>HttpStorageMedium</c> (Flowthru ≥ 0.21). <c>FetchCombos</c> reads this and projects it to the
  /// lean <see cref="MagicAtlas.Ast.Tests.Data._02_Intermediate.Schemas.Combo"/> work-list; the host's
  /// <c>UseHttp</c> conditional-GET cache (under <c>_01_Raw/Datasets/External/.http-cache</c>) means a
  /// fresh clone fetches the dump once and reuses it for 24h — no manual curl, ever. The narrow
  /// <see cref="CsbVariantsDump"/> schema drops CSB's bloat (image URIs, prices, card-state) on read;
  /// the raw bytes are never committed, only our derived edge fixtures are.
  /// </summary>
  public IItem<CsbVariantsDump> CsbVariantsRaw =>
    CreateItem(() =>
      Item.Of<CsbVariantsDump>("CsbVariantsRaw")
        .Json()
        .AtPath("https://json.commanderspellbook.com/variants.json")
        .Build()
    );
}
