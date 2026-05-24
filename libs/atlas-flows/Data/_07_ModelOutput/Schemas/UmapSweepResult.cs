using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Tidy long-form result of a UMAP hyperparameter sweep: one row per
/// (sweep_point, level-where-measured, metric). The combo is identified by the params plus a
/// human-readable <see cref="SweepId"/> (e.g. <c>"n=15,d=0.1"</c> or
/// <c>"n=25,d=0.0,sw=0.7"</c>) for easy filtering and pivoting downstream.
/// </summary>
[FlowthruSchema]
public partial record UmapSweepResult
{
  [SerializedLabel("sweep_id")]
  public required string SweepId { get; init; }

  /// <summary><c>"2d"</c> for the 5D→2D sweep, <c>"5d"</c> for the HD→5D sweep.</summary>
  [SerializedLabel("sweep_type")]
  public required string SweepType { get; init; }

  [SerializedLabel("n_neighbors")]
  public required int NNeighbors { get; init; }

  [SerializedLabel("min_dist")]
  public required double MinDist { get; init; }

  /// <summary>Only meaningful for <c>sweep_type=="5d"</c> (which sweeps supervision_weight too).
  /// Defaults to 0 for 2D-only sweep rows.</summary>
  [SerializedLabel("supervision_weight")]
  public double SupervisionWeight { get; init; }

  /// <summary>Where the metric was measured: <c>"5d"</c>, <c>"2d"</c>, or aggregate.</summary>
  [SerializedLabel("level")]
  public required string Level { get; init; }

  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  [SerializedLabel("value")]
  public required double Value { get; init; }

  /// <summary>Wall-clock seconds for this combo's UMAP runs (across all stages). Helpful for
  /// understanding whether the sweep is GPU- or CPU-bound.</summary>
  [SerializedLabel("runtime_seconds")]
  public required double RuntimeSeconds { get; init; }
}
