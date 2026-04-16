using Flowthru.Core.Data;
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
  public IItem<RulesStructure> ParsedRules =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<RulesStructure>(
          label: "ParsedRules",
          filePath: $"{_basePath}/_03_Primary/Datasets/rules-structure.json"
        )
    );

  /// <summary>
  /// Parsed glossary as term-definition pairs.
  /// </summary>
  public IItem<GlossaryEntries> ParsedGlossary =>
    CreateItem(
      () =>
        ItemFactory.Single.Json<GlossaryEntries>(
          label: "ParsedGlossary",
          filePath: $"{_basePath}/_03_Primary/Datasets/glossary.json"
        )
    );

  /// <summary>
  /// Filtered card core data (analysis-relevant fields).
  /// Persisted to disk as JSON.
  /// </summary>
  public IItem<IEnumerable<CardCoreData>> FilteredCardCoreData =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<CardCoreData>(
          label: "FilteredCardCoreData",
          filePath: $"{_basePath}/_03_Primary/Datasets/filtered-cards-core.json"
        )
    );

  /// <summary>
  /// Filtered card metadata (non-analysis fields).
  /// Persisted to disk as JSON (metadata is not flat tabular data).
  /// </summary>
  public IItem<IEnumerable<CardMetadata>> FilteredCardMetadata =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<CardMetadata>(
          label: "FilteredCardMetadata",
          filePath: $"{_basePath}/_03_Primary/Datasets/filtered-cards-metadata.json"
        )
    );

  /// <summary>
  /// Minimal oracle-text projection fed to the Python embedding step.
  /// Memory-only — exists solely to decouple CardCoreData (which has non-Arrow-friendly
  /// fields like <c>decimal Cmc</c>) from the Python subprocess handoff.
  /// </summary>
  public IItem<IEnumerable<OracleInput>> OracleInputs =>
    CreateItem(
      () => ItemFactory.Enumerable.Memory<OracleInput>(label: "OracleInputs")
    );

  /// <summary>
  /// 2D UMAP projection of oracle-text BERT embeddings — consumed by the atlas-api.
  /// Persisted to the shared <c>dumps/</c> folder so the API can load without a direct DB coupling.
  /// </summary>
  public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
    CreateItem(
      () =>
        ItemFactory.Enumerable.Json<AtlasPoint>(
          label: "AtlasPoints",
          filePath: $"{_basePath}/../../../dumps/atlas-points.json"
        )
    );
}
