using Flowthru.Data.Catalog;
using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Primary data catalog entries (Layer 3). Domain-cleansed, business-keyed, model-agnostic
/// tables — the single source of truth for downstream consumers.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Filtered card core data (analysis-relevant fields). Persisted to disk as JSON.
  /// </summary>
  public IItem<IEnumerable<CardCoreData>> FilteredCardCoreData =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardCoreData>>("FilteredCardCoreData")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/filtered-cards-core.json")
        .Build()
    );

  /// <summary>
  /// Filtered card metadata (non-analysis fields). Persisted to disk as JSON.
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
  /// barrel detection. Currently has no downstream consumer beyond barrel-detection's own
  /// internals; kept as a foundational reference artifact.
  /// </summary>
  public IItem<KeywordVocabulary> KeywordVocabulary =>
    CreateItem(() =>
      Item.Of<KeywordVocabulary>("KeywordVocabulary")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/keyword-vocabulary.json")
        .Build()
    );

  /// <summary>
  /// One row per oracle-text line. The pipeline's central join key: every downstream artifact
  /// (encoded vectors, atlas points) keys on <c>LineId</c> and reaches back to <c>CardId</c>
  /// via this table.
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
  /// hard-negative triplets (tier 3) derived from glossary/CR auto-extraction, oracle-text
  /// reminder paraphrases, and template-based seed triplets. All signal is MTG-derived; no
  /// manual curated overrides. See <see cref="TrainingPair"/>.
  /// </summary>
  public IItem<IEnumerable<TrainingPair>> TrainingPairs =>
    CreateItem(() =>
      Item.Of<IEnumerable<TrainingPair>>("TrainingPairs")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/training-pairs.json")
        .Build()
    );

  /// <summary>
  /// Same as <see cref="TrainingPairs"/>, but with hard negatives mined via base-model
  /// k-NN attached to every previously negative-less pair. The fine-tune step consumes this
  /// instead of <see cref="TrainingPairs"/> so MNR loss sees real contrastive triplets
  /// rather than relying on random in-batch sampling. See <c>mine_hard_negatives.py</c>
  /// for the mining procedure and citations.
  /// </summary>
  public IItem<IEnumerable<TrainingPair>> TrainingPairsMined =>
    CreateItem(() =>
      Item.Of<IEnumerable<TrainingPair>>("TrainingPairsMined")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/training-pairs-mined.json")
        .Build()
    );

  /// <summary>Persisted encoder cache — one row per unique oracle-text string. EmbedOracleText
  /// dedups OracleLines.Text, runs the model once per unique text, writes the result here. The
  /// 2D UMAP step broadcasts cached vectors back to per-line rows via OracleLines join.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTexts =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTexts")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts.parquet")
        .Build()
    );

  /// <summary>Sibling of <see cref="EncodedTexts"/> produced under the BASE (un-fine-tuned)
  /// embedding model. Lets the FineTuneEval flow A/B-compare the geometry of the corpus under
  /// each model variant. Not consumed by the explorer pipeline.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTextsBase =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTextsBase")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts-base.parquet")
        .Build()
    );

  /// <summary>Encoder cache for the union of (anchor, positive, negative) strings appearing in
  /// <see cref="TrainingPairs"/>, encoded under the FINE-TUNED model. Many of these strings
  /// (glossary definitions, CR section bodies) are not in the oracle-line corpus and therefore
  /// not in <see cref="EncodedTexts"/>, so they need their own cached encoding for the
  /// objective-tier health metrics.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTrainingTextsFineTuned =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTrainingTextsFineTuned")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-training-texts-finetuned.parquet")
        .Build()
    );

  /// <summary>Sibling of <see cref="EncodedTrainingTextsFineTuned"/> under the BASE model.
  /// Together they let FineTuneEval compare per-training-pair cosines and triplet margins
  /// across the two model variants.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTrainingTextsBase =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTrainingTextsBase")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-training-texts-base.parquet")
        .Build()
    );

  /// <summary>A small deterministic sample (~3k rows) of <see cref="EncodedTexts"/> used for
  /// FineTuneEval geometry-tier metrics. Sampling upstream of the eval step keeps the Python
  /// step's JSON-marshalled input under the System.Text.Json size limit (passing the full
  /// ~30k-row encoded cache by value blows that cap).</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTextsSampled =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTextsSampled")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts-sampled.parquet")
        .Build()
    );

  /// <summary>Base-model sibling of <see cref="EncodedTextsSampled"/> — same sample size,
  /// same row indices into the source corpus so geometry comparison is over identical lines.</summary>
  public IItem<IEnumerable<EncodedText>> EncodedTextsBaseSampled =>
    CreateItem(() =>
      Item.Of<IEnumerable<EncodedText>>("EncodedTextsBaseSampled")
        .Parquet()
        .AtPath($"{_basePath}/_03_Primary/Datasets/encoded-texts-base-sampled.parquet")
        .Build()
    );

  /// <summary>2D atlas points (unsupervised UMAP projection of EncodedTexts). The atlas viz
  /// surface — consumed by Reporting.BuildAtlasPlot.</summary>
  public IItem<IEnumerable<AtlasPoint>> AtlasPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<AtlasPoint>>("AtlasPoints")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/atlas-points.json")
        .Build()
    );
}
