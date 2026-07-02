using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// One row per label-free fidelity metric comparing the HD encoder space vs the 2D atlas.
/// Computed by <c>evaluate_atlas_fidelity</c>. Used as a regression detector for the
/// explorer-mode atlas — if these numbers drift downward across pipeline runs, the 2D
/// projection has lost faithfulness to its source HD topology.
/// </summary>
/// <remarks>
/// Current metrics:
/// <list type="bullet">
///   <item><b>trustworthiness_k10</b> [0, 1], higher better: are 2D k-NNs also HD k-NNs?
///     Low = false neighbors.</item>
///   <item><b>continuity_k10</b> [0, 1], higher better: are HD k-NNs also 2D k-NNs?
///     Low = torn neighborhoods.</item>
///   <item><b>card_jaccard_k10</b> [0, 1], higher better: per-card jaccard of k-NN card sets
///     at HD vs 2D.</item>
///   <item><b>density_spearman_k10</b> [-1, 1], higher better (but expected near 0 for default
///     UMAP): per-line Spearman correlation of HD local density vs 2D local density. UMAP
///     flattens density by default; DensMAP lifts this dramatically. Values &gt;0.3 mean visual
///     density is a real signal an explorer can rely on.</item>
///   <item><b>scale_stability</b> [0, ~0.1], lower better: std-dev of trustworthiness across
///     k = 5, 10, 25, 50. Low (&lt;0.03) = consistent neighborhood preservation across the
///     zoom levels an explorer might mentally use; high = projection is good at one scale
///     and misleading at others.</item>
/// </list>
/// </remarks>
[FlowthruSchema]
public partial record AtlasFidelityMetric
{
  [SerializedLabel("metric")]
  public required string Metric { get; init; }

  [SerializedLabel("value")]
  public required double Value { get; init; }
}
