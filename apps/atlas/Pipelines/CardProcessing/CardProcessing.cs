using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.CardProcessing.Nodes;

namespace MagicAtlas.Pipelines.CardProcessing;

/// <summary>
/// Pipeline for processing raw Scryfall data into strongly-typed schemas.
/// Transforms card symbols and cards from raw JSON DTOs to processed records with enums.
/// </summary>
public static class CardProcessing
{
  /// <summary>
  /// Configuration parameters for the card processing pipeline.
  /// </summary>
  public record Params
  {
    /// <summary>
    /// Configuration options for filtering cards.
    /// </summary>
    public FilterAndSplitCardsNode.FilterOptions FilterOptions { get; init; } = new();
  }

  /// <summary>
  /// Creates the card processing pipeline.
  /// </summary>
  /// <param name="catalog">The data catalog containing input and output entries.</param>
  /// <param name="parameters">Configuration parameters for the pipeline.</param>
  /// <returns>A configured pipeline that processes and filters card data.</returns>
  public static Pipeline Create(Catalog catalog, Params parameters)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "ParseCardSymbols",
        description: "Transform raw card symbols to processed symbols with ManaColor enums",
        transform: ParseCardSymbolsNode.Create(),
        input: catalog.RawCardSymbols,
        output: catalog.ProcessedCardSymbols
      );

      pipeline.AddNode(
        label: "ParseCards",
        description: "Transform raw cards to processed cards with strong types (35K+ cards, memory only)",
        transform: ParseCardsNode.Create(),
        input: catalog.RawCards,
        output: catalog.ProcessedCards
      );

      pipeline.AddNode(
        label: "FilterAndSplitCards",
        description: "Filter cards by configurable criteria and split into core data and metadata",
        transform: FilterAndSplitCardsNode.Create(parameters.FilterOptions),
        input: catalog.ProcessedCards,
        output: (catalog.FilteredCardCoreData, catalog.FilteredCardMetadata)
      );

      pipeline.AddNode(
        label: "RefineOracleText",
        description: "Refine oracle text by removing parentheticals and categorizing abilities",
        transform: RefineOracleTextNode.Create(),
        input: catalog.FilteredCardCoreData,
        output: catalog.RefinedOracleText
      );

      pipeline.AddNode(
        label: "EmbeddingModelOracleInput",
        description: "Flatten refined oracle text into individual entries for embedding model input",
        transform: CreateEmbeddingModelOracleInputNode.Create(),
        input: catalog.RefinedOracleText,
        output: catalog.EmbeddingModelOracleInput
      );
    });
  }
}
