using Flowthru.Data;
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
  public ICatalogEntry<CardSymbolDictionary> ProcessedCardSymbols =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<CardSymbolDictionary>(
          label: "ProcessedCardSymbols",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/card-symbols.json"
        )
    );

  /// <summary>
  /// Processed cards with strong types.
  /// Stored in memory only (not persisted to disk due to size).
  /// Contains 35,000+ card objects with full type safety.
  /// </summary>
  public ICatalogEntry<CardCollection> ProcessedCards =>
    GetOrCreateEntry(() => CatalogEntries.Single.Memory<CardCollection>(label: "ProcessedCards"));

  /// <summary>
  /// Introduction section.
  /// </summary>
  public ICatalogEntry<string> Intro =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "Intro",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/intro.txt"
        )
    );

  /// <summary>
  /// Table of contents section.
  /// </summary>
  public ICatalogEntry<string> TableOfContents =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "TableOfContents",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/toc.txt"
        )
    );

  /// <summary>
  /// Rules section (numbered rules only).
  /// </summary>
  public ICatalogEntry<string> RulesText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "RulesText",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/rules.txt"
        )
    );

  /// <summary>
  /// Glossary section.
  /// </summary>
  public ICatalogEntry<string> GlossaryText =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "GlossaryText",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/glossary.txt"
        )
    );

  /// <summary>
  /// Credits section.
  /// </summary>
  public ICatalogEntry<string> Credits =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Text(
          label: "Credits",
          filePath: $"{_basePath}/_02_Intermediate/Datasets/RulesSections/credits.txt"
        )
    );
}
