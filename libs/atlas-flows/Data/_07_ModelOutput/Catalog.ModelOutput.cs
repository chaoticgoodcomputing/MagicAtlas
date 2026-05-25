using Flowthru.Data.Catalog;
using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Model evaluation outputs (Layer 7). Diagnostics about the encoded/projected artifacts that
/// inform whether the explorer atlas is healthy.
/// </summary>
public partial class Catalog
{
  /// <summary>
  /// Diagnostic snapshot of <c>ProjectOracleLinesNode</c>'s barrel-detection pass — barrel/
  /// borderline counts plus sample lines. One report per pipeline run; model-agnostic (operates
  /// on raw oracle text, not embeddings).
  /// </summary>
  public IItem<BarrelDetectionReport> BarrelDetectionReport =>
    CreateItem(() =>
      Item.Of<BarrelDetectionReport>("BarrelDetectionReport")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/barrel-detection-report.json")
        .Build()
    );

  /// <summary>
  /// Label-free fidelity scorecard for the explorer atlas — measures HD↔2D neighborhood
  /// preservation (trustworthiness, continuity, per-card k-NN jaccard). Acts as the
  /// regression detector for the explorer-mode pipeline: when these numbers drift, the
  /// projection has lost faithfulness to its source HD topology. See
  /// <see cref="AtlasFidelityMetric"/>.
  /// </summary>
  public IItem<IEnumerable<AtlasFidelityMetric>> AtlasFidelityMetrics =>
    CreateItem(() =>
      Item.Of<IEnumerable<AtlasFidelityMetric>>("AtlasFidelityMetrics")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/atlas-fidelity-metrics.json")
        .Build()
    );

  /// <summary>
  /// Base-vs-fine-tuned comparison scorecard for the embedding model. Geometry-tier metrics
  /// (pairwise-cosine spread, hubness) and objective-tier metrics (per-source triplet margins,
  /// positive/negative cosine means) measured under each model variant. Lets you decide
  /// "is the fine-tune helping or hurting?" empirically rather than by intuition.
  /// See <see cref="FineTuneHealthMetric"/>.
  /// </summary>
  public IItem<IEnumerable<FineTuneHealthMetric>> FineTuneHealthMetrics =>
    CreateItem(() =>
      Item.Of<IEnumerable<FineTuneHealthMetric>>("FineTuneHealthMetrics")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/fine-tune-health-metrics.json")
        .Build()
    );
}
