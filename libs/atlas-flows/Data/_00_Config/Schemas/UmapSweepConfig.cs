using Flowthru.Data.Schema;

namespace MagicAtlas.Data._00_Config.Schemas;

/// <summary>
/// Configuration for the 5D→2D UMAP hyperparameter sweep. The step takes the cartesian product
/// of <see cref="NNeighborsGrid"/> × <see cref="MinDistGrid"/> and runs an unsupervised 2D UMAP
/// per combo (with the existing 5D ClusteringEmbeddings as input), emitting one
/// <see cref="MagicAtlas.Data._07_ModelOutput.Schemas.UmapSweepResult"/> row per
/// (combo, level, metric).
/// </summary>
[FlowthruSchema]
public partial record UmapSweep2DConfig
{
  [SerializedLabel("n_neighbors_grid")]
  public required List<int> NNeighborsGrid { get; init; }

  [SerializedLabel("min_dist_grid")]
  public required List<double> MinDistGrid { get; init; }

  /// <summary>K for K-NN-based metrics (knn_purity, trustworthiness, continuity). Default 10.</summary>
  [SerializedLabel("knn_k")]
  public int KnnK { get; init; } = 10;

  /// <summary>Sample size cap for trustworthiness/continuity (sklearn builds N×N distances).
  /// Default 5000.</summary>
  [SerializedLabel("trust_sample_n")]
  public int TrustSampleN { get; init; } = 5000;
}

/// <summary>
/// Configuration for the HD→5D supervised UMAP hyperparameter sweep. Cartesian product over
/// three axes: <see cref="NNeighborsGrid"/> × <see cref="MinDistGrid"/> ×
/// <see cref="SupervisionWeightGrid"/>. Each combo runs HD→5D supervised; metrics report 5D
/// quality (silhouette, knn_purity vs canonical labels).
/// </summary>
[FlowthruSchema]
public partial record UmapSweep5DConfig
{
  [SerializedLabel("n_neighbors_grid")]
  public required List<int> NNeighborsGrid { get; init; }

  [SerializedLabel("min_dist_grid")]
  public required List<double> MinDistGrid { get; init; }

  [SerializedLabel("supervision_weight_grid")]
  public required List<double> SupervisionWeightGrid { get; init; }

  /// <summary>K for K-NN-based metrics. Default 10.</summary>
  [SerializedLabel("knn_k")]
  public int KnnK { get; init; } = 10;

  /// <summary>Sample size cap for trustworthiness/continuity. Default 5000.</summary>
  [SerializedLabel("trust_sample_n")]
  public int TrustSampleN { get; init; } = 5000;
}
