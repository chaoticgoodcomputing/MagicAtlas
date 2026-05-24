using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// One row per oracle line, naming the single canonical archetype the line is most confidently
/// attributed to. Derived from <see cref="OracleLineCanonicalAssignment"/> by picking the
/// highest-confidence assignment per line (tie-broken by source priority: anchor > inferred >
/// fallback-exemplar > fallback-all).
/// </summary>
/// <remarks>
/// <para>
/// This is the "ground truth" the pipeline treats as canonical for downstream reporting and
/// clustering benchmarks. HDBSCAN/UMAP clusters are scored against this assignment as a
/// candidate clustering, not the other way around.
/// </para>
/// <para>
/// Lines with no canonical attribution don't appear in this table — downstream consumers
/// should treat their absence as a sentinel for "uncategorized" rather than fabricating one.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record LinePrimaryCanonical
{
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  [SerializedLabel("canonical_slug")]
  public required string CanonicalSlug { get; init; }

  /// <summary>Top-level family extracted from the colon-delimited slug (everything before the
  /// first colon). Used for coarser visual grouping when the full slug space is too granular —
  /// e.g. all <c>tribal:*</c> lines share <c>tribal</c>, all <c>removal:*</c> share <c>removal</c>.</summary>
  [SerializedLabel("canonical_family")]
  public required string CanonicalFamily { get; init; }

  [SerializedLabel("confidence")]
  public required double Confidence { get; init; }

  [SerializedLabel("source")]
  public required string Source { get; init; }
}
