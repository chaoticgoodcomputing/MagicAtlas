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

  // Commander Spellbook's combo dump (variants.json, ~510 MB) is NOT a catalog item: it is curled
  // to a gitignored static input (_01_Raw/Datasets/External/csb-variants.json) and stream-read by
  // FetchCombosStep. Flowthru's HTTP catalog medium can't serve it — the .Json() singleton builder
  // doesn't route https:// through UseHttp (treated as a file path) — a gap to push upstream. The
  // raw bytes are never committed; only our derived edge fixtures are ours to keep.
}
