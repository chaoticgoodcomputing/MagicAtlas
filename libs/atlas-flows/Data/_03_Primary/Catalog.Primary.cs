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
  /// Minimal oracle-text projection fed to the Python embedding step. Persisted (not memory-only)
  /// because the Clustering flow's <c>generate_ctfidf_labels</c> step re-reads the per-fragment
  /// text long after the embedding step has run, and we want that step to be runnable in
  /// isolation (e.g. when re-labeling with a different backend).
  /// </summary>
  public IItem<IEnumerable<OracleInput>> OracleInputs =>
    CreateItem(() =>
      Item.Of<IEnumerable<OracleInput>>("OracleInputs")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/oracle-inputs.json")
        .Build()
    );

  /// <summary>
  /// Sentence-transformer (all-MiniLM-L6-v2, 384-dim float32) embeddings of each oracle-text
  /// fragment. The shared intermediate between the 2D-UMAP display reduction and the 5D-UMAP +
  /// HDBSCAN clustering reduction — BERT encode runs once, both reductions read this file.
  /// Parquet (~80 MB) because JSON would be unworkable at ~50K × 384 floats.
  /// </summary>
  public IItem<IEnumerable<BertEmbedding>> BertEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<BertEmbedding>>("BertEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/bert-embeddings.parquet")
        .Build()
    );

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

  /// <summary>
  /// Per-point cluster assignments produced by the Clustering flow's HDBSCAN step over a 5D-UMAP
  /// reduction of <see cref="BertEmbeddings"/>. One row per fragment, joinable to
  /// <see cref="AtlasPoints"/> / <see cref="OracleInputs"/> on <c>point_id</c>.
  /// </summary>
  public IItem<IEnumerable<ClusterAssignment>> ClusterAssignments =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterAssignment>>("ClusterAssignments")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/cluster-assignments.json")
        .Build()
    );

  /// <summary>
  /// Per-cluster labels. Backend-agnostic schema (see <see cref="ClusterLabel"/>) so this single
  /// catalog item can hold output from c-TF-IDF today and an LLM labeler tomorrow without
  /// downstream code changes.
  /// </summary>
  public IItem<IEnumerable<ClusterLabel>> ClusterLabels =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterLabel>>("ClusterLabels")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/cluster-labels.json")
        .Build()
    );
}
