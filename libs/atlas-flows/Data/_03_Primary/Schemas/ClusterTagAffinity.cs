using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Per-cluster candidate set for the labeler. Joins each cluster's centroid (in encoded-text
/// space) against the unified tag-centroid pool (exemplar + Scryfall), surfaces the top-K
/// candidates with scores and provenance, and includes a sample of representative oracle lines
/// for the labeler prompt.
/// </summary>
/// <remarks>
/// <para>
/// Flat schema (scalars + parallel <see cref="List{T}"/> columns). The candidate-* lists are
/// parallel arrays of equal length; index <c>i</c> across all four describes one candidate.
/// This shape avoids the Flowthru Python marshaller's nested-POCO restriction while still
/// surfacing the candidate-set semantics clearly to the downstream labeler.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ClusterTagAffinity
{
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }

  [SerializedLabel("cluster_size")]
  public required int ClusterSize { get; init; }

  /// <summary>Candidate tag slugs, ranked by cosine similarity (highest first). Length == K.</summary>
  [SerializedLabel("candidate_slugs")]
  public required List<string> CandidateSlugs { get; init; }

  /// <summary>Display names for the candidates, parallel to <see cref="CandidateSlugs"/>.</summary>
  [SerializedLabel("candidate_names")]
  public required List<string> CandidateNames { get; init; }

  /// <summary>Provenance per candidate: <c>"exemplar"</c> or <c>"scryfall"</c>.</summary>
  [SerializedLabel("candidate_sources")]
  public required List<string> CandidateSources { get; init; }

  /// <summary>Cosine similarity between the cluster centroid and each candidate tag centroid,
  /// parallel to <see cref="CandidateSlugs"/>.</summary>
  [SerializedLabel("candidate_scores")]
  public required List<double> CandidateScores { get; init; }

  /// <summary>Oracle-text lines closest to the cluster centroid, included in the labeler's
  /// prompt as concrete examples of the cluster's content.</summary>
  [SerializedLabel("sample_lines")]
  public required List<string> SampleLines { get; init; }
}
