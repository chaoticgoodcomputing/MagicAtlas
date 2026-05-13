using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Generic per-cluster label, designed to be backend-agnostic. The current c-TF-IDF labeler
/// populates <see cref="Label"/> + <see cref="Keywords"/> and leaves <see cref="Description"/>
/// null; a future LLM-based labeler reads the same <see cref="ClusterAssignment"/> input and
/// emits this same shape, allowing reporting / API consumers to swap label sources without code
/// changes. <see cref="Source"/> is metadata, not contract — keep its value out of presentation
/// logic.
/// </summary>
[FlowthruSchema]
public partial record ClusterLabel
{
  /// <summary>Matches <see cref="ClusterAssignment.ClusterId"/>. Noise cluster (<c>-1</c>) is
  /// labeled too, typically with a sentinel like "(noise)".</summary>
  [SerializedLabel("cluster_id")]
  public required int ClusterId { get; init; }

  /// <summary>
  /// Primary display label. For c-TF-IDF this is the top n-gram (or a short comma-joined head of
  /// <see cref="Keywords"/>); for LLM-generated labels it's a human-authored short phrase like
  /// "Card draw triggers".
  /// </summary>
  [SerializedLabel("label")]
  public required string Label { get; init; }

  /// <summary>Longer prose — typically populated by LLM labelers, often null for statistical
  /// labelers. Reporting may surface this in a richer tooltip.</summary>
  [SerializedLabel("description")]
  public string? Description { get; init; }

  /// <summary>
  /// JSON-encoded array of top-N tokens, e.g. <c>'["draw","card","hand"]'</c>. The structured
  /// top-N is the c-TF-IDF labeler's native output; an LLM labeler can synthesize equivalent
  /// theme words from its prompt context. Stored as a single JSON string rather than
  /// <c>string[]</c> because Flowthru's Python ↔ C# Arrow marshaller (as of 0.17.4) only
  /// handles scalar property types — typed arrays trigger a <c>NotSupportedException</c> in
  /// the Arrow decoder. Consumers (Reporting, future API) <c>JSON.parse</c> this to recover
  /// the list.
  /// </summary>
  [SerializedLabel("keywords")]
  public required string Keywords { get; init; }

  /// <summary>Points in this cluster — denormalized so consumers can sort/filter without joining
  /// against <see cref="ClusterAssignment"/>.</summary>
  [SerializedLabel("size")]
  public required int Size { get; init; }

  /// <summary>Backend identifier — e.g. <c>"c-tf-idf"</c>, <c>"gpt-4o-mini"</c>, <c>"claude-opus-4-7"</c>.
  /// Metadata for diffs and version tracking, not for branching display logic.</summary>
  [SerializedLabel("source")]
  public required string Source { get; init; }

  /// <summary>Library or model version pinned for reproducibility (e.g. <c>"sklearn-1.4.0"</c>,
  /// <c>"gpt-4o-mini-2024-07-18"</c>). Optional.</summary>
  [SerializedLabel("source_version")]
  public string? SourceVersion { get; init; }
}
