namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// The resource/event kinds a port can emit or consume (mast-interaction ADR-0001, §4). Seeded to
/// the kinds the canonical Chatterfang × Pitiless gold needs; extended one vertical slice at a time.
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
/// A resource a port emits or consumes. <see cref="Subject"/> is the game object the resource
/// concerns — the dying creature, the created token — and is <c>null</c> for scalar resources
/// (mana, generic counters), where the join is a kind-match with no <c>ObjectFilter</c> overlap.
/// </summary>
public sealed record Resource(ResourceKind Kind, ObjectFilter? Subject = null);

/// <summary>
/// One authored family edge (mast-interaction ADR-0001, §3 — the source-of-truth grammar): a
/// directed <c>fromLabel → toLabel</c> on a resource, joined by its edge family. Authored as JSON;
/// the engine expands it over the derived ports whose labels match (never a blanket cartesian
/// product) and keeps only pairs where the operator-join holds.
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

/// <summary>
/// An addressable ability sub-tree in a role (mast-interaction ADR-0001, §2). <see cref="Identity"/>
/// is the canonical-subtree hash (<c>CanonicalJson.Hash</c>) so a port has one identity across
/// processes. A port projects its emit / consume / intercept resource sets.
/// </summary>
public sealed record Port
{
  public required string Card { get; init; }
  public required string Label { get; init; }
  public required string Identity { get; init; }
  public IReadOnlyList<Resource> Emits { get; init; } = [];
  public IReadOnlyList<Resource> Consumes { get; init; } = [];
  public IReadOnlyList<Resource> Intercepts { get; init; } = [];

  public override string ToString() => $"{Card}::{Label}";
}

/// <summary>
/// A directed port→port edge on a resource, carrying the operator's verdicts: <see cref="Overlap"/>
/// (can the subjects coincide — the flow-edge prune) and <see cref="Reliability"/> (does it fire for
/// every instance the producer makes — the net-resource accounting input). <see cref="Reason"/> is
/// the operator's provenance for an Unknown/conditional verdict.
/// </summary>
public sealed record InteractionEdge
{
  public required Port From { get; init; }
  public required Port To { get; init; }
  public required ResourceKind Resource { get; init; }
  public required EdgeFamily Family { get; init; }
  public required FilterRelation Overlap { get; init; }
  public required Trilean Reliability { get; init; }
  public string? Reason { get; init; }

  /// <summary>Green iff the subjects provably overlap AND the handoff is reliable; Red on a dark hop; else Amber.</summary>
  public CertaintyTier Tier =>
    Overlap == FilterRelation.Disjoint ? CertaintyTier.Red
    : Overlap == FilterRelation.Overlaps && Reliability == Trilean.Yes ? CertaintyTier.Green
    : CertaintyTier.Amber;
}

/// <summary>A reconstructed loop over the port-instance graph: its hops, the limiting certainty tier, and why.</summary>
public sealed record InteractionCycle
{
  public required IReadOnlyList<InteractionEdge> Edges { get; init; }

  /// <summary>The worst hop sets the cycle's tier (a loop is only as certain as its least-certain hop).</summary>
  public CertaintyTier Tier => Edges.Count == 0 ? CertaintyTier.Green : Edges.Max(e => e.Tier);

  /// <summary>The hop that limits the tier — and its operator <see cref="InteractionEdge.Reason"/> (what to build to promote it).</summary>
  public InteractionEdge? LimitingHop =>
    Edges.OrderByDescending(e => (int)e.Tier).ThenBy(e => e.From.Label, StringComparer.Ordinal).FirstOrDefault();
}
