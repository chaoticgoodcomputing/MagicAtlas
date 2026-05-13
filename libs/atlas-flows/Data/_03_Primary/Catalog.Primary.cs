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

  /// <summary>
  /// 5D UMAP-reduced view of <see cref="BertEmbeddings"/> — the shared intermediate between the
  /// Clustering flow's HDBSCAN step and the ModelEvaluations flow's centroid-distance metric.
  /// Hoisted out of the clusterer so a model change can be evaluated without re-running the
  /// (slow) UMAP, and so HDBSCAN parameters can be retuned in isolation.
  /// </summary>
  public IItem<IEnumerable<ClusteringEmbedding>> ClusteringEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusteringEmbedding>>("ClusteringEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/clustering-embeddings.parquet")
        .Build()
    );

  /// <summary>
  /// Card-level oracle text with reminder parentheticals intact — the input shape the FineTune
  /// flow's training-pair builder needs to extract reminder-text paraphrase pairs. Sibling to
  /// <see cref="OracleInputs"/>, which is fragment-level with parentheticals stripped.
  /// </summary>
  public IItem<IEnumerable<CardOracleText>> CardOracleTexts =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardOracleText>>("CardOracleTexts")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/card-oracle-texts.json")
        .Build()
    );

  /// <summary>
  /// Training corpus for the fine-tuned embedding model — positive pairs (tier 1+2) and
  /// hard-negative triplets (tier 3) merged from glossary/CR auto-extraction, oracle-text
  /// reminder paraphrases, template-based triplet mining, and curated overrides.
  /// See <see cref="TrainingPair"/>.
  /// </summary>
  public IItem<IEnumerable<TrainingPair>> TrainingPairs =>
    CreateItem(() =>
      Item.Of<IEnumerable<TrainingPair>>("TrainingPairs")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/training-pairs.json")
        .Build()
    );

  // -------- Fine-tuned variant siblings --------
  // Same schemas as the default-variant items above; separate catalog entries so both pipelines
  // can run in parallel and so downstream consumers can target one variant explicitly. Renaming
  // the default items would cascade through the atlas-api and the public AtlasPoints path, so we
  // leave the unqualified names as the default-variant aliases and namespace only the new
  // sibling items.

  /// <summary>BERT vectors produced by the fine-tuned MTG-tuned embedding model.
  /// Same schema as <see cref="BertEmbeddings"/>; downstream steps treat them interchangeably.</summary>
  public IItem<IEnumerable<BertEmbedding>> FineTunedBertEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<BertEmbedding>>("FineTunedBertEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-bert-embeddings.parquet")
        .Build()
    );

  /// <summary>5D UMAP of <see cref="FineTunedBertEmbeddings"/>. Same schema as
  /// <see cref="ClusteringEmbeddings"/>.</summary>
  public IItem<IEnumerable<ClusteringEmbedding>> FineTunedClusteringEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusteringEmbedding>>("FineTunedClusteringEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-clustering-embeddings.parquet")
        .Build()
    );

  /// <summary>2D atlas points produced from <see cref="FineTunedBertEmbeddings"/>. Same schema
  /// as <see cref="AtlasPoints"/>.</summary>
  public IItem<IEnumerable<AtlasPoint>> FineTunedAtlasPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<AtlasPoint>>("FineTunedAtlasPoints")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-atlas-points.json")
        .Build()
    );

  /// <summary>Per-point cluster assignments from clustering the fine-tuned variant. Same schema
  /// as <see cref="ClusterAssignments"/>.</summary>
  public IItem<IEnumerable<ClusterAssignment>> FineTunedClusterAssignments =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterAssignment>>("FineTunedClusterAssignments")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-cluster-assignments.json")
        .Build()
    );

  /// <summary>Per-cluster labels for the fine-tuned variant. Same schema as
  /// <see cref="ClusterLabels"/>.</summary>
  public IItem<IEnumerable<ClusterLabel>> FineTunedClusterLabels =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterLabel>>("FineTunedClusterLabels")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-cluster-labels.json")
        .Build()
    );
}
