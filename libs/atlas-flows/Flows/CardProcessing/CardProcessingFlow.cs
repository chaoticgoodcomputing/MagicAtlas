using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._01_Raw.Schemas;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.CardProcessing.Nodes;

namespace MagicAtlas.Flows.CardProcessing;

/// <summary>
/// Pipeline for processing raw Scryfall data into strongly-typed schemas.
/// Reads the <c>_01_Raw</c> items produced by the <c>Ingest</c> flow, parses card symbols and
/// cards into typed records, and applies a configurable filter that splits each card into
/// analysis core data and metadata.
/// </summary>
public static class CardProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog, CardProcessingFlowConfig config)
  {
    var filterTransform = FilterAndSplitCardsNode.Create(config.FilterOptions);

    return FlowBuilder.CreateFlow("CardProcessing", pipeline =>
    {
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
