using Flowthru.Core.Flows;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.CardProcessing.Nodes;

namespace MagicAtlas.Pipelines.CardProcessing;

/// <summary>
/// Pipeline for processing raw Scryfall data into strongly-typed schemas.
/// Transforms card symbols and cards from raw JSON DTOs to processed records with enums.
/// </summary>
public static class CardProcessing
{
  public record Params
  {
    public FilterAndSplitCardsNode.FilterOptions FilterOptions { get; init; } = new();
  }

  public static Flow Create(Catalog catalog, Params parameters)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      pipeline.AddStep(
        label: "ParseCardSymbols",
        transform: ParseCardSymbolsNode.Create(),
        input: catalog.RawCardSymbols,
        output: catalog.ProcessedCardSymbols
      );

      pipeline.AddStep(
        label: "ParseCards",
        transform: ParseCardsNode.Create(),
        input: catalog.RawCards,
        output: catalog.ProcessedCards
      );

      pipeline.AddStep(
        label: "FilterAndSplitCards",
        transform: FilterAndSplitCardsNode.Create(parameters.FilterOptions),
        input: catalog.ProcessedCards,
        output: (catalog.FilteredCardCoreData, catalog.FilteredCardMetadata)
      );
    });
  }
}
