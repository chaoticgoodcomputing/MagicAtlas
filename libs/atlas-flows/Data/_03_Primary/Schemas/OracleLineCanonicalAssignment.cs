using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Per-line attribution of an OracleLine to a curated canonical archetype. Built by the
/// canonical-line-assignment step, which resolves the card-level → line-level granularity gap
/// in Scryfall's tag taxonomy: Scryfall tags a *card* but a multi-line card may only have one
/// of its lines actually representing the tagged archetype.
/// </summary>
/// <remarks>
/// <para>
/// The assignment is built by (1) anchoring per-canonical centroids from cards with exactly
/// one natural oracle line (where the card→tag mapping is unambiguous), then (2) for each
/// line of a multi-line tagged card, picking the closest of that card's tags' anchors above
/// a threshold.
/// </para>
/// <para>
/// Each row records WHY the line was attributed:
/// <list type="bullet">
/// <item><c>anchor</c> — line came from a single-line card whose tag could only apply here.</item>
/// <item><c>inferred</c> — multi-line card; line was assigned to its closest anchor among
/// the card's own tags, with cosine ≥ <c>LineAnchorThreshold</c>.</item>
/// <item><c>fallback-exemplar</c> — canonical had no single-line anchor; used the curated
/// exemplar centroid as the anchor instead.</item>
/// <item><c>fallback-all</c> — neither single-line population nor exemplar available; line
/// was attributed under the original card-level "all lines" rule (least confident).</item>
/// </list>
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record OracleLineCanonicalAssignment
{
  [SerializedLabel("line_id")]
  public required Guid LineId { get; init; }

  [SerializedLabel("canonical_slug")]
  public required string CanonicalSlug { get; init; }

  /// <summary>Cosine similarity between the line's embedding and the canonical's anchor
  /// centroid (or <c>1.0</c> for direct single-line anchors).</summary>
  [SerializedLabel("confidence")]
  public required double Confidence { get; init; }

  /// <summary>One of: <c>"anchor"</c>, <c>"inferred"</c>, <c>"fallback-exemplar"</c>,
  /// <c>"fallback-all"</c>. See class remarks for semantics.</summary>
  [SerializedLabel("source")]
  public required string Source { get; init; }
}
