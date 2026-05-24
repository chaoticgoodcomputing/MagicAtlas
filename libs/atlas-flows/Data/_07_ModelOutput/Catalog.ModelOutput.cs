using Flowthru.Data.Catalog;
using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Model evaluation outputs (Layer 7). One row per evaluated assertion per model variant —
/// post-gut, only the fine-tuned variant remains and base-model comparison plumbing was removed.
/// </summary>
public partial class Catalog
{
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

  /// <summary>Quality scorecard for HDBSCAN clustering measured against the canonical
  /// line-attribution ground truth. Per-cluster rows + corpus-wide row.</summary>
  public IItem<IEnumerable<ClusterCanonicalBenchmark>> ClusterCanonicalBenchmark =>
    CreateItem(() =>
      Item.Of<IEnumerable<ClusterCanonicalBenchmark>>("ClusterCanonicalBenchmark")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/cluster-canonical-benchmark.json")
        .Build()
    );

  /// <summary>Per-canonical 2D-placement scorecard — radii, dispersion, centroid-based
  /// silhouette, overlap rate. One row per canonical + an overall row (slug = "*").</summary>
  public IItem<IEnumerable<CanonicalPlacementMetric>> CanonicalPlacementMetrics =>
    CreateItem(() =>
      Item.Of<IEnumerable<CanonicalPlacementMetric>>("CanonicalPlacementMetrics")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/canonical-placement-metrics.json")
        .Build()
    );

  /// <summary>Cross-level projection-quality scorecard — HD/5D/2D × Exploration/Exploitation
  /// × 8 metrics in tidy long form. See <see cref="ProjectionQualityMetric"/>.</summary>
  public IItem<IEnumerable<ProjectionQualityMetric>> ProjectionQualityMetrics =>
    CreateItem(() =>
      Item.Of<IEnumerable<ProjectionQualityMetric>>("ProjectionQualityMetrics")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/projection-quality-metrics.json")
        .Build()
    );

  /// <summary>Results of the 5D→2D UMAP sweep — tidy long form, one row per
  /// (sweep_point, level, metric). Tuning-only artifact; not consumed by the production atlas.</summary>
  public IItem<IEnumerable<UmapSweepResult>> UmapSweep2DResults =>
    CreateItem(() =>
      Item.Of<IEnumerable<UmapSweepResult>>("UmapSweep2DResults")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/umap-sweep-2d-results.json")
        .Build()
    );

  /// <summary>Results of the HD→5D supervised UMAP sweep — tidy long form. Per-combo metrics
  /// at the 5D layer (where supervision lives) plus the downstream 2D layer (with default 2D
  /// hyperparams) for end-to-end visibility.</summary>
  public IItem<IEnumerable<UmapSweepResult>> UmapSweep5DResults =>
    CreateItem(() =>
      Item.Of<IEnumerable<UmapSweepResult>>("UmapSweep5DResults")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/umap-sweep-5d-results.json")
        .Build()
    );
}
