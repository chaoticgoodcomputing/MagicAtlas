using Flowthru.Core.Data;
using MagicAtlas.Data._02_Intermediate.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Intermediate data catalog entries (Layer 2).
/// Contains typed representations of raw source data without structural transformation.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Processed card symbols with strong types.
  /// Persisted to disk as JSON.
  /// </summary>
  public IItem<CardSymbolDictionary> ProcessedCardSymbols =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<CardSymbolDictionary>(
          label: "ProcessedCardSymbols",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/card-symbols.json"
        )
    );

  /// <summary>
  /// Processed cards with strong types.
  /// Stored in memory only (not persisted to disk due to size).
  /// Contains 35,000+ card objects with full type safety.
  /// </summary>
  public IItem<CardCollection> ProcessedCards =>
    CreateItem(() => ItemFactory.Single.Memory<CardCollection>(label: "ProcessedCards"));

  /// <summary>
  /// Introduction section.
  /// </summary>
  public IItem<string> Intro =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "Intro",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/intro.txt"
        )
    );

  /// <summary>
  /// Table of contents section.
  /// </summary>
  public IItem<string> TableOfContents =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "TableOfContents",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/toc.txt"
        )
    );

  /// <summary>
  /// Rules section (numbered rules only).
  /// </summary>
  public IItem<string> RulesText =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "RulesText",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/rules.txt"
        )
    );

  /// <summary>
  /// Glossary section.
  /// </summary>
  public IItem<string> GlossaryText =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "GlossaryText",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/glossary.txt"
        )
    );

  /// <summary>
  /// Credits section.
  /// </summary>
  public IItem<string> Credits =>
    CreateItem(
      () =>
        ItemFactory.Single.Text(
          label: "Credits",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/credits.txt"
        )
    );
}
