using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Per-canonical placement quality metrics in the 2D atlas. Surfaces "is this annotation in a
/// sensible spot relative to its member lines, and are the lines themselves tight or smeared?"
/// One row per canonical (with at least one attributed line), plus an overall corpus row
/// (<c>canonical_slug = "*"</c>) holding aggregate stats.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Silhouette"/> is the centroid-based variant: per-line, <c>(b − a) / max(a, b)</c>
/// where <c>a</c> is L2 distance to the line's own canonical centroid and <c>b</c> is the
/// distance to the nearest other canonical centroid. Averaged per canonical. Range −1..+1
/// (+1 = perfectly clustered, 0 = on a boundary, −1 = misplaced).
/// </para>
/// <para>
/// <see cref="OverlapRate"/> is the fraction of this canonical's member lines that are closer
/// to *another* canonical's centroid than to their own — a directly visual diagnostic for "the
/// annotation is sitting in the wrong neighborhood."
/// </para>
/// <para>
/// On the overall row: <c>n_lines</c> is the corpus total; <c>mean_radius</c>/<c>dispersion</c>
/// are means over the per-canonical means; <c>silhouette</c> is the corpus-wide mean per-line
/// silhouette; <c>overlap_rate</c> is the corpus-wide mean overlap; <c>nearest_canonical</c>
/// is left empty.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record CanonicalPlacementMetric
{
  [SerializedLabel("canonical_slug")]
  public required string CanonicalSlug { get; init; }

  [SerializedLabel("n_lines")]
  public required int NLines { get; init; }

  [SerializedLabel("centroid_x")]
  public required double CentroidX { get; init; }

  [SerializedLabel("centroid_y")]
  public required double CentroidY { get; init; }

  [SerializedLabel("mean_radius")]
  public required double MeanRadius { get; init; }

  [SerializedLabel("median_radius")]
  public required double MedianRadius { get; init; }

  [SerializedLabel("radius_p90")]
  public required double RadiusP90 { get; init; }

  [SerializedLabel("dispersion")]
  public required double Dispersion { get; init; }

  [SerializedLabel("silhouette")]
  public required double Silhouette { get; init; }

  [SerializedLabel("nearest_canonical")]
  public required string NearestCanonical { get; init; }

  [SerializedLabel("nearest_distance")]
  public required double NearestDistance { get; init; }

  [SerializedLabel("overlap_rate")]
  public required double OverlapRate { get; init; }
}
