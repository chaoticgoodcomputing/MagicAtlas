using Flowthru.Data.Catalog;
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
    CreateItem(() =>
      Item.Of<RulesStructure>("ParsedRules")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/rules-structure.json")
        .Build()
    );

  /// <summary>
  /// Parsed glossary as term-definition pairs.
  /// </summary>
  public IItem<GlossaryEntries> ParsedGlossary =>
    CreateItem(() =>
      Item.Of<GlossaryEntries>("ParsedGlossary")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/glossary.json")
        .Build()
    );

  /// <summary>
  /// Filtered card core data (analysis-relevant fields).
  /// Persisted to disk as JSON.
  /// </summary>
  public IItem<IEnumerable<CardCoreData>> FilteredCardCoreData =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardCoreData>>("FilteredCardCoreData")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/filtered-cards-core.json")
        .Build()
    );

  /// <summary>
  /// Filtered card metadata (non-analysis fields).
  /// Persisted to disk as JSON (metadata is not flat tabular data).
  /// </summary>
  public IItem<IEnumerable<CardMetadata>> FilteredCardMetadata =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardMetadata>>("FilteredCardMetadata")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/filtered-cards-metadata.json")
        .Build()
    );

  /// <summary>
  /// Minimal oracle-text projection fed to the Python embedding step.
  /// Memory-only — exists solely to decouple CardCoreData (which has non-Arrow-friendly
  /// fields like <c>decimal Cmc</c>) from the Python subprocess handoff.
  /// </summary>
  public IItem<IEnumerable<OracleInput>> OracleInputs =>
    CreateItem(() => Item.Of<IEnumerable<OracleInput>>("OracleInputs").Memory().Build());

  /// <summary>
  /// 2D UMAP projection of oracle-text BERT embeddings — consumed by the atlas-api.
  /// Lives in the harness's Primary layer under traditional Flowthru conventions; the API loads
  /// it via its <c>Atlas:AtlasPointsPath</c> setting, decoupled from DB ingestion.
  /// </summary>
  public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<AtlasPoint>>("AtlasPoints")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/atlas-points.json")
        .Build()
    );
}
