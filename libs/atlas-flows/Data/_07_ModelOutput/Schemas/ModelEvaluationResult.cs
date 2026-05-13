using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// One row per evaluated assertion per model variant. Distance values are the squared L2
/// distances between centroids in the 5D <c>ClusteringEmbeddings</c> space — i.e. the same
/// metric HDBSCAN saw, so eval results stay coupled to what the clusterer actually used.
/// </summary>
/// <remarks>
/// A future addition could include <c>distance_within_a</c> / <c>distance_within_b</c>
/// (mean intra-group distance) to surface assertions that pass on centroid alignment but fail
/// on tightness — e.g. "Flying centroid is close to Menace centroid, but Flying cards are
/// scattered across half the space."
/// </remarks>
[FlowthruSchema]
public partial record ModelEvaluationResult
{
  /// <summary>Model identifier — e.g. <c>"default-minilm-l6-v2"</c>,
  /// <c>"mtg-mpnet-v1"</c>. Lets a single ModelEvaluation item hold rows from multiple
  /// variants if we collapse the per-variant items into one in the future.</summary>
  [SerializedLabel("model_variant")]
  public required string ModelVariant { get; init; }

  [SerializedLabel("assertion_name")]
  public required string AssertionName { get; init; }

  /// <summary>Squared L2 distance between the centroid of group A and the centroid of group B.</summary>
  [SerializedLabel("distance_a_b")]
  public required double DistanceAB { get; init; }

  /// <summary>Squared L2 distance between the centroid of group A and the centroid of the
  /// baseline group.</summary>
  [SerializedLabel("distance_a_baseline")]
  public required double DistanceABaseline { get; init; }

  /// <summary>Whether the assertion's expectation (<c>closer_than</c> / <c>farther_than</c>)
  /// holds for this model.</summary>
  [SerializedLabel("pass")]
  public required bool Pass { get; init; }

  /// <summary>Number of fragments matched into group A.</summary>
  [SerializedLabel("n_a")]
  public required int NA { get; init; }

  /// <summary>Number of fragments matched into group B.</summary>
  [SerializedLabel("n_b")]
  public required int NB { get; init; }

  /// <summary>Number of fragments matched into the baseline group.</summary>
  [SerializedLabel("n_baseline")]
  public required int NBaseline { get; init; }
}
