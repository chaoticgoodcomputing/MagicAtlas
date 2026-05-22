using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Per-Scryfall-keyword cluster diagnostic — one row per keyword. For every synthetic-keyword
/// cohort, the report records which cluster they fell into, the 2D centroid of the cohort, a
/// sample of cards that ended up in a different cluster, and the nearest other keywords by
/// centroid distance. Catches "Haste anchor ended up in the same cluster as Trample"-type
/// weirdness without requiring assertion-suite math.
/// </summary>
/// <remarks>
/// <para>
/// Two catalog items use this schema — one per model variant. The variant identity is encoded
/// in the catalog item name (<c>KeywordClusterReport</c> vs <c>FineTunedKeywordClusterReport</c>)
/// rather than as a row column, so the file shape stays narrow.
/// </para>
/// <para>
/// Flat-tabular by necessity: Python step outputs can't carry nested POCOs through Flowthru's
/// Arrow marshaller (see [[flowthru-python-step-input-marshaller]]). <c>OutlierSample</c> is a
/// JSON-encoded string column following the same pattern as <c>ClusterLabel.Keywords</c>.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record KeywordClusterReport
{
  [SerializedLabel("keyword")]
  public required string Keyword { get; init; }

  /// <summary>Cluster id that the majority of synthetic lines for this keyword fell into.</summary>
  [SerializedLabel("anchor_cluster_id")]
  public required int AnchorClusterId { get; init; }

  /// <summary>Display label of the anchor cluster (from <c>ClusterLabel.Label</c>).</summary>
  [SerializedLabel("anchor_cluster_label")]
  public required string AnchorClusterLabel { get; init; }

  /// <summary>2D centroid of this keyword's synthetic lines in atlas-display coordinates.</summary>
  [SerializedLabel("centroid_x")]
  public required double CentroidX { get; init; }

  [SerializedLabel("centroid_y")]
  public required double CentroidY { get; init; }

  /// <summary>Total synthetic lines for this keyword (one per card that has it as a Scryfall keyword).</summary>
  [SerializedLabel("n_member_lines")]
  public required int NMemberLines { get; init; }

  /// <summary>Synthetic lines that landed in a cluster other than <c>AnchorClusterId</c>.</summary>
  [SerializedLabel("n_outliers")]
  public required int NOutliers { get; init; }

  /// <summary>Top-K nearest other keywords by 2D centroid distance, ascending distance,
  /// JSON-encoded as a string array (e.g. <c>["Flash", "Haste", "Trample", "Vigilance", "Ward"]</c>).</summary>
  [SerializedLabel("top_neighbor_keywords")]
  public required string TopNeighborKeywords { get; init; }

  /// <summary>Sample of cards whose synthetic line landed in a non-anchor cluster, JSON-encoded
  /// as a string array of <c>{card_id, card_name, actual_cluster_id}</c> objects.</summary>
  [SerializedLabel("outlier_sample")]
  public required string OutlierSample { get; init; }
}
