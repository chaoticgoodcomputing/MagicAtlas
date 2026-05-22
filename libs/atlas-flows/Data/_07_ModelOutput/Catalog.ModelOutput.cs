using Flowthru.Data.Catalog;
using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Model evaluation outputs (Layer 7). One row per evaluated assertion per model variant —
/// see <see cref="ModelEvaluationResult"/>. Two per-variant catalog items rather than one
/// merged item so each pipeline run writes its own report and stale variants don't bleed into
/// a combined file; cross-variant comparisons happen at read time downstream.
/// </summary>
public partial class Catalog
{
  /// <summary>Eval results for the default off-the-shelf model. Produced by ModelEvaluations
  /// when the flow is run against <see cref="ClusteringEmbeddings"/>.</summary>
  public IItem<IEnumerable<ModelEvaluationResult>> ModelEvaluation =>
    CreateItem(() =>
      Item.Of<IEnumerable<ModelEvaluationResult>>("ModelEvaluation")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/model-evaluation.json")
        .Build()
    );

  /// <summary>Eval results for the fine-tuned model. Produced by ModelEvaluations when the
  /// flow is run against <see cref="FineTunedClusteringEmbeddings"/>.</summary>
  public IItem<IEnumerable<ModelEvaluationResult>> FineTunedModelEvaluation =>
    CreateItem(() =>
      Item.Of<IEnumerable<ModelEvaluationResult>>("FineTunedModelEvaluation")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/fine-tuned-model-evaluation.json")
        .Build()
    );

  /// <summary>
  /// Diagnostic snapshot of <c>ProjectOracleLinesNode</c>'s barrel-detection pass — barrel/
  /// borderline counts plus sample lines. One report per pipeline run; not per-variant
  /// because barrel detection is model-agnostic (operates on raw oracle text, not embeddings).
  /// </summary>
  public IItem<BarrelDetectionReport> BarrelDetectionReport =>
    CreateItem(() =>
      Item.Of<BarrelDetectionReport>("BarrelDetectionReport")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/barrel-detection-report.json")
        .Build()
    );

  /// <summary>Post-clustering per-keyword diagnostic for the default model — one row per
  /// Scryfall keyword with anchor cluster, member count, top neighbors, and outlier sample.</summary>
  public IItem<IEnumerable<KeywordClusterReport>> KeywordClusterReport =>
    CreateItem(() =>
      Item.Of<IEnumerable<KeywordClusterReport>>("KeywordClusterReport")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/keyword-cluster-report.json")
        .Build()
    );

  /// <summary>Fine-tuned-model counterpart to <see cref="KeywordClusterReport"/>.</summary>
  public IItem<IEnumerable<KeywordClusterReport>> FineTunedKeywordClusterReport =>
    CreateItem(() =>
      Item.Of<IEnumerable<KeywordClusterReport>>("FineTunedKeywordClusterReport")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/fine-tuned-keyword-cluster-report.json")
        .Build()
    );
}
