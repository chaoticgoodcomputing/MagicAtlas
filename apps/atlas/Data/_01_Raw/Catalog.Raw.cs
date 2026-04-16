using Flowthru.Core.Data;
using MagicAtlas.Data._01_Raw.Schemas;

namespace MagicAtlas.Data;

public partial class Catalog
{
  /// <summary>
  /// Raw MTG comprehensive rules text file.
  ///
  /// Retrieved from https://magic.wizards.com/en/rules
  /// </summary>
  public IItem<string> RawRules =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "RawRules",
          filePath: $"{_basePath}/_01_Raw/Datasets/mtg-rules.txt"
        )
    );

  /// <summary>
  /// Raw Scryfall oracle card symbols JSON.
  ///
  /// Retrieved from Scryfall's bulk data export at https://scryfall.com/docs/api/card-symbols
  /// </summary>
  public IItem<RawScryfallCardSymbolList> RawCardSymbols =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<RawScryfallCardSymbolList>(
          label: "RawCardSymbols",
          filePath: $"{_basePath}/_01_Raw/Datasets/oracle-card-symbols.json"
        )
    );

  /// <summary>
  /// Raw Scryfall oracle cards JSON.
  ///
  /// Retrieved from Scryfall's bulk data export at https://scryfall.com/docs/api/bulk-data
  /// </summary>
  public IItem<IEnumerable<RawScryfallCard>> RawCards =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<RawScryfallCard>(
          label: "RawCards",
          filePath: $"{_basePath}/_01_Raw/Datasets/oracle-cards.json"
        )
    );
}
