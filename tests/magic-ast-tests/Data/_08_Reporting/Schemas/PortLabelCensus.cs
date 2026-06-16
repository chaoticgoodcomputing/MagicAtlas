using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Port-label census — a DIAGNOSTIC (not a gate; diagnostics live in Flowthru, never the NUnit suite).
/// Tests the "analytical-chemistry" premise behind the two-layer cycle engine
/// (<c>libs/mast-interaction/docs/two-layer-cycle-engine.md</c>): ports are the bounded *atoms* of
/// gameplay, cards the combinatorial *molecules*, so the distinct-label space should stay far below the
/// card count (<c>N_labels ≪ N_cards</c>) — and the cycle graph should be built over the atoms, not the
/// molecules. Emitted by the <c>PortLabelCensus</c> flow over the full parsed corpus; re-run as parser
/// coverage grows. The card:label ratio is the health metric for the assumption.
/// </summary>
[FlowthruSchema]
public partial record PortLabelCensus
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>Corpus cards with non-empty oracle text that were parsed + projected.</summary>
  [SerializedLabel("cardsWalked")]
  public int CardsWalked { get; init; }

  /// <summary>Distinct port labels across the corpus (the dedup of cards → atoms).</summary>
  [SerializedLabel("distinctLabels")]
  public int DistinctLabels { get; init; }

  /// <summary>Distinct labels whose role forms an interaction edge — the node count of the cycle graph.</summary>
  [SerializedLabel("cycleRelevantLabels")]
  public int CycleRelevantLabels { get; init; }

  /// <summary>Distinct inert/coarse labels (no edge: modify/evasion/coarse emit:&lt;x&gt;/unprojected triggers).</summary>
  [SerializedLabel("inertLabels")]
  public int InertLabels { get; init; }

  [SerializedLabel("cardsPerDistinctLabel")]
  public double CardsPerDistinctLabel { get; init; }

  [SerializedLabel("cardsPerCycleRelevantLabel")]
  public double CardsPerCycleRelevantLabel { get; init; }

  /// <summary>Distinct-label count per role (first label segment), edge-forming roles flagged.</summary>
  [SerializedLabel("byRole")]
  public required IReadOnlyList<RoleLabelCount> ByRole { get; init; }

  /// <summary>The most-reused labels — one atom, many cards (the dedup payoff, concrete).</summary>
  [SerializedLabel("topLabels")]
  public required IReadOnlyList<LabelCardCount> TopLabels { get; init; }
}

/// <summary>Distinct port-label count for one role.</summary>
[FlowthruSchema]
public partial record RoleLabelCount
{
  [SerializedLabel("role")]
  public required string Role { get; init; }

  [SerializedLabel("distinctLabels")]
  public int DistinctLabels { get; init; }

  [SerializedLabel("edgeForming")]
  public bool EdgeForming { get; init; }
}

/// <summary>How many distinct cards project one label.</summary>
[FlowthruSchema]
public partial record LabelCardCount
{
  [SerializedLabel("label")]
  public required string Label { get; init; }

  [SerializedLabel("cardCount")]
  public int CardCount { get; init; }
}
