using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Size + shape overview of the MATERIALIZED interaction port graph — the union graph the
/// <c>InteractionTriage</c> flow builds over every parse-ready combo's cards (a port is a card
/// property, deduped per card; see <c>MaterializeCardEdgesStep</c> / <c>InteractionUnion</c>). The
/// node/edge counts here describe the engine's actual reconstruction graph, complementing the
/// <see cref="PortLabelCensus"/> (which counts the distinct-LABEL vocabulary across the whole parsed
/// corpus, not the materialized graph's nodes/edges).
/// <para>
/// "Port" = a distinct (card, label) instance that participates in ≥1 materialized edge (an emit
/// feeding, or a cost/trigger consuming). Edges are the engine's card-defined + rules-defined hops,
/// tier-tagged. Use this for a quick read of how big + how dense the interaction graph is, and how the
/// edges split by certainty tier / flow family / flowing resource.
/// </para>
/// </summary>
[FlowthruSchema]
public partial record PortGraphMetrics
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>Distinct (card, label) port instances participating in ≥1 edge — the graph's NODE count.</summary>
  [SerializedLabel("totalPorts")]
  public int TotalPorts { get; init; }

  /// <summary>Materialized port→port edges — the graph's EDGE count (card-defined + rules-defined hops).</summary>
  [SerializedLabel("totalEdges")]
  public int TotalEdges { get; init; }

  /// <summary>Distinct cards spanned by the graph (the parse-ready combo cards that materialized any edge).</summary>
  [SerializedLabel("distinctCards")]
  public int DistinctCards { get; init; }

  /// <summary>Distinct port LABELS present as graph nodes (the "atom" vocabulary actually wired, vs the
  /// whole-corpus census in <see cref="PortLabelCensus"/>).</summary>
  [SerializedLabel("distinctPortLabels")]
  public int DistinctPortLabels { get; init; }

  /// <summary>Emit-side port nodes (label starts <c>emit:</c>) — the producers.</summary>
  [SerializedLabel("emitPorts")]
  public int EmitPorts { get; init; }

  /// <summary>Consume-side port nodes (cost/trigger labels — <c>pay:</c>/<c>sac:</c>/<c>tap:</c>/<c>trigger:</c>/…) — the consumers.</summary>
  [SerializedLabel("consumePorts")]
  public int ConsumePorts { get; init; }

  /// <summary>Edges ÷ ports — the graph's mean degree (a rough density read; higher ⇒ more tightly wired).</summary>
  [SerializedLabel("edgesPerPort")]
  public double EdgesPerPort { get; init; }

  /// <summary>Edge count by certainty tier (Green = certified, Amber = conditional, Red = pruned/disjoint).</summary>
  [SerializedLabel("edgesByTier")]
  public required IReadOnlyList<LabeledCount> EdgesByTier { get; init; }

  /// <summary>Edge count by flow family (the rules-defined arm that produced the edge).</summary>
  [SerializedLabel("edgesByFamily")]
  public required IReadOnlyList<LabeledCount> EdgesByFamily { get; init; }

  /// <summary>Edge count by flowing resource (read off the source emit label — token/mana/life/…).</summary>
  [SerializedLabel("edgesByResource")]
  public required IReadOnlyList<LabeledCount> EdgesByResource { get; init; }
}

/// <summary>A (category, count) pair — one bucket of a port-graph breakdown, ordered by descending count.</summary>
[FlowthruSchema]
public partial record LabeledCount
{
  [SerializedLabel("label")]
  public string Label { get; init; } = "";

  [SerializedLabel("count")]
  public int Count { get; init; }
}
