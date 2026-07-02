namespace MagicAST.Interaction;

/// <summary>
/// The resource/event kinds a port emits or consumes (mast-interaction ADR-0001 §4). Seeded to the
/// kinds the canonical golds need; extended one vertical slice at a time. The single-role port model
/// (ADR-0002) carries the resource via the colon-label + an <c>ObjectFilter</c> subject rather than a
/// kind enum, but this enum still names the authored family-edge resources and seeds the resource-kind
/// label facet (<c>PortLabel</c>).
/// </summary>
public enum ResourceKind
{
  Mana,
  Token,
  Counter,
  Death,
  EntersBattlefield,
  LeavesBattlefield,
  Sacrifice,
  Cast,
}

/// <summary>
/// One authored family edge (mast-interaction ADR-0001 §3 — the source-of-truth label grammar): a
/// directed <c>fromLabel → toLabel</c> on a resource, joined by its edge family. Authored as JSON;
/// the label-level viz subplot reads it. (ADR-0002's <c>PortGraphEngine</c> <em>derives</em> the flow
/// grammar from colon-labels and no longer consumes this; it remains the authored label-grammar view.)
/// </summary>
public sealed record FamilyEdge
{
  [System.Text.Json.Serialization.JsonPropertyName("from")]
  public required string From { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("to")]
  public required string To { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("resource")]
  public required ResourceKind Resource { get; init; }

  [System.Text.Json.Serialization.JsonPropertyName("family")]
  public required EdgeFamily Family { get; init; }
}

/// <summary>Flow = a resource handoff (A emits R, B consumes R). Modifier = A rewrites B's emission (a doubler/replacement).</summary>
public enum EdgeFamily
{
  Flow,
  Modifier,
}

/// <summary>Certainty of an edge or cycle. Ordered worst-last so a cycle's tier is the max over its hops.</summary>
public enum CertaintyTier
{
  Green = 0, // certain
  Amber = 1, // closes only through an Unknown / conditional region
  Red = 2, // a dark hop — an expected edge that does not hold
}
