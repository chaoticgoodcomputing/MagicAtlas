using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// One row per HDBSCAN cluster discovered in the unsupervised 5D embedding space, characterized
/// as a candidate for archetype curation. The discover→curate→validate loop:
/// </summary>
/// <list type="number">
///   <item>Run HDBSCAN on UNSUPERVISED 5D — discover what the embedding naturally groups.</item>
///   <item>For each cluster, compute coverage by existing archetypes + c-TF-IDF distinctive
///     tokens + medoid + sample lines.</item>
///   <item>Classify each cluster as NEW / REFINE / MERGE / SPLIT / COVERED and surface as a
///     recommendation.</item>
///   <item>User reviews via <c>scripts/review_archetype_recommendations.py</c> and updates
///     <c>canonical-archetypes.json</c> by copying sample lines as seed prototypes.</item>
/// </list>
/// <remarks>
/// The point: prototypes for new archetypes come FROM THE DATA they're meant to attract, not
/// from human guesses. Re-runs of the loop progressively discover increasingly fine-grained
/// structure as the existing archetypes' coverage tightens.
/// </remarks>
[FlowthruSchema]
public partial record ArchetypeRecommendation
{
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }

  [SerializedLabel("n_members")]
  public required int NMembers { get; init; }

  /// <summary>Mean cosine of cluster members to their HD centroid. High (&gt;0.85) = tight cluster
  /// that's likely a real archetype; low (&lt;0.7) = diffuse cluster, likely a noise grouping.</summary>
  [SerializedLabel("cohesion")]
  public required double Cohesion { get; init; }

  /// <summary>NEW / REFINE / MERGE / SPLIT / COVERED. See remarks on
  /// <see cref="ArchetypeRecommendation"/>. SPLIT fires when an existing archetype is the
  /// closest match for multiple mutually-distinct clusters whose internal cohesion exceeds
  /// their match strength — strong evidence the archetype bundles several semantic sub-regions
  /// and should be split into N archetypes.</summary>
  [SerializedLabel("verdict")]
  public required string Verdict { get; init; }

  /// <summary>Pipe-joined top-K c-TF-IDF tokens for this cluster vs the corpus.</summary>
  [SerializedLabel("ctfidf_tokens")]
  public required string CTfIdfTokens { get; init; }

  /// <summary>Slug of the closest existing archetype (by HD centroid cosine).</summary>
  [SerializedLabel("closest_archetype_slug")]
  public required string ClosestArchetypeSlug { get; init; }

  /// <summary>Cosine of this cluster's HD centroid to <see cref="ClosestArchetypeSlug"/>'s
  /// prototype centroid. &gt;0.85: well covered. 0.7–0.85: refine candidate. &lt;0.7: NEW candidate.</summary>
  [SerializedLabel("closest_archetype_cosine")]
  public required double ClosestArchetypeCosine { get; init; }

  /// <summary>Suggested slug for a new archetype (derived from top c-TF-IDF token).
  /// Placeholder — user should rename.</summary>
  [SerializedLabel("suggested_slug")]
  public required string SuggestedSlug { get; init; }

  /// <summary>Text of the cluster's medoid line — the line with smallest summed cosine distance
  /// to other members. Most representative concrete example.</summary>
  [SerializedLabel("medoid_line_text")]
  public required string MedoidLineText { get; init; }

  /// <summary>10 sample lines from the cluster (5 closest to centroid + 5 random) ready to be
  /// copy-pasted as prototype seeds. Separated by " | " for tidy storage.</summary>
  [SerializedLabel("sample_lines_joined")]
  public required string SampleLinesJoined { get; init; }

  /// <summary>For SPLIT verdicts: pipe-joined cluster IDs of sibling clusters that also have
  /// <see cref="ClosestArchetypeSlug"/> as their closest archetype. Empty string for other
  /// verdicts. Lets the review script render all sibling clusters together as one split
  /// recommendation.</summary>
  [SerializedLabel("split_sibling_cluster_ids")]
  public string SplitSiblingClusterIds { get; init; } = "";

  /// <summary>Ring diagnostic: tokens overrepresented in cluster members at cosine-quartile
  /// p25-50 (the second-closest 25% to the centroid) compared to the core (p0-25). Useful for
  /// spotting near-core bleed: tokens here that don't appear in the cluster's overall identity
  /// are early-warning false positives.</summary>
  [SerializedLabel("ring_p25_50_tokens")]
  public string RingP25_50Tokens { get; init; } = "";

  /// <summary>Ring diagnostic: tokens overrepresented in the p50-75 cosine quartile vs the core
  /// (p0-25). Mid-ring drift — material the cluster is mid-confidence about.</summary>
  [SerializedLabel("ring_p50_75_tokens")]
  public string RingP50_75Tokens { get; init; } = "";

  /// <summary>Ring diagnostic: tokens overrepresented in the periphery (bottom 25% by cosine)
  /// vs the core (p0-25). These are the loudest false-positive signals — periphery tokens
  /// reveal what the cluster is picking up that doesn't belong to its identity.</summary>
  [SerializedLabel("ring_p75_99_tokens")]
  public string RingP75_99Tokens { get; init; } = "";
}
