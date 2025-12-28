using Flowthru.Data;
using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Primary data catalog entries (Layer 3).
/// Contains domain-specific data models cleansed and transformed for MTG analysis.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Parsed hierarchical rules structure.
  /// </summary>
  public ICatalogEntry<RulesStructure> ParsedRules =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<RulesStructure>(
          label: "ParsedRules",
          filePath: $"{_basePath}/_03_Primary/Datasets/rules-structure.json"
        )
    );

  /// <summary>
  /// Parsed glossary as term-definition pairs.
  /// </summary>
  public ICatalogEntry<GlossaryEntries> ParsedGlossary =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Single.Json<GlossaryEntries>(
          label: "ParsedGlossary",
          filePath: $"{_basePath}/_03_Primary/Datasets/glossary.json"
        )
    );

  /// <summary>
  /// Filtered card core data (analysis-relevant fields).
  /// Persisted to disk as JSON.
  /// </summary>
  public ICatalogEntry<IEnumerable<CardCoreData>> FilteredCardCoreData =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<CardCoreData>(
          label: "FilteredCardCoreData",
          filePath: $"{_basePath}/_03_Primary/Datasets/filtered-cards-core.json"
        )
    );

  /// <summary>
  /// Filtered card metadata (non-analysis fields).
  /// Persisted to disk as JSON (metadata is not flat tabular data).
  /// </summary>
  public ICatalogEntry<IEnumerable<CardMetadata>> FilteredCardMetadata =>
    GetOrCreateEntry(
      () =>
        CatalogEntries.Enumerable.Json<CardMetadata>(
          label: "FilteredCardMetadata",
          filePath: $"{_basePath}/_03_Primary/Datasets/filtered-cards-metadata.json"
        )
    );
}
