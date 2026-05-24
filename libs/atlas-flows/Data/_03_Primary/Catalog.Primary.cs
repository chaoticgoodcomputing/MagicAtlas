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

  /// <summary>Persisted encoder cache — one row per unique oracle-text string. EmbedOracleText
  /// dedups OracleLines.Text, runs the model once per unique text, writes the result here. The
  /// 5D / 2D UMAP steps broadcast cached vectors back to per-line rows via OracleLines join.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTexts =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTexts")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts.parquet")
        .Build()
    );

  /// <summary>5D UMAP of EncodedTexts (supervised by canonical labels). Structured intermediate
  /// between HD and 2D — see Clustering.ReduceToFiveD for the rationale.</summary>
  public IItem<IEnumerable<ClusteringEmbedding>> ClusteringEmbeddings =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusteringEmbedding>>("ClusteringEmbeddings")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/clustering-embeddings.parquet")
        .Build()
    );

  /// <summary>2D atlas points (unsupervised projection of ClusteringEmbeddings). The atlas viz
  /// surface — consumed by the atlas-api via Atlas:AtlasPointsPath.</summary>
  public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<AtlasPoint>>("AtlasPoints")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/atlas-points.json")
        .Build()
    );

  /// <summary>Per-line HDBSCAN cluster assignments over ClusteringEmbeddings.</summary>
  public IItem<IEnumerable<ClusterAssignment>> ClusterAssignments =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterAssignment>>("ClusterAssignments")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/cluster-assignments.json")
        .Build()
    );

  /// <summary>Per-cluster c-TF-IDF labels. Backend-agnostic ClusterLabel shape.</summary>
  public IItem<IEnumerable<ClusterLabel>> ClusterLabels =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterLabel>>("ClusterLabels")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/cluster-labels.json")
        .Build()
    );

  // ── Tag-labeling artifacts ────────────────────────────────────────────────────────

  /// <summary>Hand-curated archetype centroids — each TagExemplar's description+examples
  /// embedded and mean-pooled.</summary>
  public IItem<IEnumerable<TagCentroid>> ExemplarTagCentroids =>
    CreateItem(() =>
      Item.Of<IEnumerable<TagCentroid>>("ExemplarTagCentroids")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/exemplar-tag-centroids.parquet")
        .Build()
    );

  /// <summary>Scryfall-tag centroids — for each curated otag canonical, the mean of all
  /// tagged cards' line-level embeddings. Pairs with ExemplarTagCentroids as the candidate
  /// pool for cluster labeling.</summary>
  public IItem<IEnumerable<TagCentroid>> ScryfallTagCentroids =>
    CreateItem(() =>
      Item.Of<IEnumerable<TagCentroid>>("ScryfallTagCentroids")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/scryfall-tag-centroids.parquet")
        .Build()
    );

  /// <summary>Line-level attribution of OracleLines to curated canonicals. Multiple attributions
  /// per line (pattern + anchor + scryfall-inference + embedding-inference + fallback). Used
  /// upstream of LinePrimaryCanonicals which picks one winner per line.</summary>
  public IItem<IEnumerable<OracleLineCanonicalAssignment>> OracleLineCanonicalAssignments =>
    CreateItem(() =>
      Item.Of<IEnumerable<OracleLineCanonicalAssignment>>("OracleLineCanonicalAssignments")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/oracle-line-canonical-assignments.parquet")
        .Build()
    );

  /// <summary>One row per oracle line with its single best canonical attribution (highest
  /// confidence wins). The "ground truth" the rest of the pipeline uses for supervision,
  /// reporting, and benchmarking.</summary>
  public IItem<IEnumerable<LinePrimaryCanonical>> LinePrimaryCanonicals =>
    CreateItem(() =>
      Item.Of<IEnumerable<LinePrimaryCanonical>>("LinePrimaryCanonicals")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/line-primary-canonicals.parquet")
        .Build()
    );

  /// <summary>Per-cluster candidate-set for the labeler — top-K tag candidates + sample
  /// lines.</summary>
  public IItem<IEnumerable<ClusterTagAffinity>> ClusterTagAffinity =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterTagAffinity>>("ClusterTagAffinity")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/cluster-tag-affinity.json")
        .Build()
    );

  /// <summary>Qwen-arbitrated cluster labels (same ClusterLabel shape as c-TF-IDF; separate
  /// catalog item so both sources can be persisted side-by-side).</summary>
  public IItem<IEnumerable<ClusterLabel>> TagAnchoredClusterLabels =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterLabel>>("TagAnchoredClusterLabels")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/tag-anchored-cluster-labels.json")
        .Build()
    );

  /// <summary>
  /// Nested tree view of the Scryfall-tag curation, with each canonical positioned in the
  /// hierarchy implied by its colon-delimited slug. The root list holds top-level archetypes
  /// (anthem, ramp, removal, tribal, …); each entry's <c>Children</c> recursively holds the
  /// sub-archetypes under it. Consumed by downstream report / test code that needs to reason
  /// over the taxonomy as a tree rather than a flat list.
  /// </summary>
  public IItem<IEnumerable<TagHierarchyNode>> TagHierarchy =>
    CreateItem(() =>
      Item.Of<IEnumerable<TagHierarchyNode>>("TagHierarchy")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/tag-hierarchy.json")
        .Build()
    );
}
