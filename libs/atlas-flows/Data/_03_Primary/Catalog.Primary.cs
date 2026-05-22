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
  /// Sorted distinct set of Scryfall keyword strings observed across the filtered card corpus.
  /// Derived from <c>FilteredCardCoreData.Keywords</c>; used by <c>ProjectOracleLines</c> for
  /// barrel detection and by the keyword-cluster reports for anchor identification.
  /// </summary>
  public IItem<KeywordVocabulary> KeywordVocabulary =>
    CreateItem(() =>
      Item.Of<KeywordVocabulary>("KeywordVocabulary")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/keyword-vocabulary.json")
        .Build()
    );

  /// <summary>
  /// One row per oracle-text line, fed to the Python embedding pipeline as the line-of-text
  /// inventory. The pipeline's central join key: every downstream artifact (encoded vectors,
  /// atlas points, cluster assignments, cluster labels) keys on <c>LineId</c> and reaches back
  /// to <c>CardId</c> via this table. Persisted (not memory-only) because reporting,
  /// clustering, and the eval suite all re-read it long after the embed step has run.
  /// </summary>
  public IItem<IEnumerable<OracleLine>> OracleLines =>
    CreateItem(() =>
      Item.Of<IEnumerable<OracleLine>>("OracleLines")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/oracle-lines.json")
        .Build()
    );

  /// <summary>
  /// Persisted encoder cache for the default-variant sentence-transformer model — one row per
  /// unique oracle-text string. <c>EmbedOracleText</c> deduplicates <see cref="OracleLines"/> by
  /// <c>Text</c>, runs the model only over the unique set, and writes the result here. The 2D /
  /// 5D UMAP steps consume <c>OracleLines + EncodedTexts</c> together and broadcast the cached
  /// vectors back to per-line rows just before jitter+UMAP. Parquet because the embedding column
  /// is a binary blob and ~30K unique strings × 384 floats would be unworkable in JSON.
  /// </summary>
  public IItem<IEnumerable<EncodedText>> EncodedTexts =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTexts")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts.parquet")
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
  /// Per-line cluster assignments produced by the Clustering flow's HDBSCAN step over a 5D-UMAP
  /// reduction of the encoded texts. One row per <see cref="OracleLines"/> row, joinable to
  /// <see cref="AtlasPoints"/> on <c>line_id</c>.
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
  /// 5D UMAP-reduced view of the encoded oracle lines — the shared intermediate between the
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
  /// <see cref="OracleLines"/>, which is line-level with parentheticals stripped.
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

  /// <summary>Persisted encoder cache for the fine-tuned variant. Same shape and rationale as
  /// <see cref="EncodedTexts"/>; downstream steps consume whichever variant matches the model
  /// they're attached to.</summary>
  public IItem<IEnumerable<EncodedText>> FineTunedEncodedTexts =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("FineTunedEncodedTexts")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-encoded-texts.parquet")
        .Build()
    );

  /// <summary>5D UMAP of <see cref="FineTunedEncodedTexts"/>. Same schema as
  /// <see cref="ClusteringEmbeddings"/>.</summary>
  public IItem<IEnumerable<ClusteringEmbedding>> FineTunedClusteringEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusteringEmbedding>>("FineTunedClusteringEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/fine-tuned-clustering-embeddings.parquet")
        .Build()
    );

  /// <summary>2D atlas points produced from <see cref="FineTunedEncodedTexts"/>. Same schema
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
