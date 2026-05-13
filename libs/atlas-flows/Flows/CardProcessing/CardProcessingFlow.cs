using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.CardProcessing.Nodes;

namespace MagicAtlas.Flows.CardProcessing;

/// <summary>
/// Pipeline for processing raw Scryfall data into strongly-typed schemas.
/// Transforms card symbols and cards from raw JSON DTOs to processed records with enums,
/// then applies a configurable filter to split outputs into analysis core data and metadata.
/// </summary>
public static class CardProcessingFlow
{
  public static BuiltFlow Create(
    Catalog catalog,
    CardProcessingFlowConfig config,
    HttpClient httpClient
  )
  {
    var filterTransform = FilterAndSplitCardsNode.Create(config.FilterOptions);

    return FlowBuilder.CreateFlow("CardProcessing", pipeline =>
    {
      // Source step: fetch Scryfall card symbology directly into the in-memory RawCardSymbols
      // slot. RawCards (the 165 MB bulk) is fetched lazily by Flowthru's HTTP-cached medium
      // when ParseCards reads it — no upstream source step required.
      pipeline.AddStep<RawScryfallCardSymbolList>(
        label: "FetchCardSymbols",
        transform: FetchCardSymbolsNode.Create(httpClient),
        outputs: catalog.RawCardSymbols
      );

      pipeline.AddStep<RawScryfallCardSymbolList, CardSymbolDictionary>(
        label: "ParseCardSymbols",
        transform: ParseCardSymbolsNode.Create(),
        inputs: catalog.RawCardSymbols,
        outputs: catalog.ProcessedCardSymbols
      );

      pipeline.AddStep<IEnumerable<RawScryfallCard>, CardCollection>(
        label: "ParseCards",
        transform: ParseCardsNode.Create(),
        inputs: catalog.RawCards,
        outputs: catalog.ProcessedCards
      );

      pipeline.AddStep<
        CardCollection,
        IEnumerable<CardCoreData>,
        IEnumerable<CardMetadata>
      >(
        label: "FilterAndSplitCards",
        transform: filterTransform,
        inputs: catalog.ProcessedCards,
        outputs: (catalog.FilteredCardCoreData, catalog.FilteredCardMetadata)
      );
    });
  }
}
