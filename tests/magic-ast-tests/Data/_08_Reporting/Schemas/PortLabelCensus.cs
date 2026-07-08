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

  /// <summary>
  /// PROJECTION pick surface — the projection-work analogue of the parse triage's
  /// <c>topYieldClusters</c>. Each entry is a card's-effect that projects to a
  /// COARSE port label (an <c>emit:&lt;x&gt;</c> fallback or <c>emit:unparsed</c>)
  /// that no flow arm reads, so the card's interaction footprint is invisible to
  /// the cycle engine <b>even though it already parses</b>. Adding one PortWalk arm
  /// for that label lights up EVERY card carrying it (projection is high-leverage —
  /// one arm, many cards — so the mass is NOT fractionally split, unlike parse
  /// yield). Ranked by <see cref="ProjectionGap.ComboPopularityMass"/>: the total
  /// popularity of the combos whose reconstruction is blocked behind this missing
  /// projection. Zero-mass entries (coarse labels on cards no combo needs) rank
  /// last. Empty when no InteractionTriage value map was available at census time.
  /// </summary>
  [SerializedLabel("topProjectionGaps")]
  public IReadOnlyList<ProjectionGap> TopProjectionGaps { get; init; } = [];
}

/// <summary>
/// One coarse/unprojected port label and the downstream combo value gated behind
/// giving it a real PortWalk flow arm — the projection pick surface's unit.
/// </summary>
[FlowthruSchema]
public partial record ProjectionGap
{
  /// <summary>The coarse port label (e.g. <c>emit:draw</c>, <c>emit:unparsed</c>).</summary>
  [SerializedLabel("label")]
  public required string Label { get; init; }

  /// <summary>Distinct corpus cards that project this coarse label.</summary>
  [SerializedLabel("cardCount")]
  public int CardCount { get; init; }

  /// <summary>How many of those cards gate at least one Commander Spellbook combo.</summary>
  [SerializedLabel("comboBlockedCards")]
  public int ComboBlockedCards { get; init; }

  /// <summary>
  /// Sum of the popularity mass of the combos gated by the cards carrying this
  /// coarse label — the projection surface's ranking key. Because one flow arm
  /// projects every card with the label, this is the (un-split) total value a
  /// single projection unit unblocks.
  /// </summary>
  [SerializedLabel("comboPopularityMass")]
  public long ComboPopularityMass { get; init; }

  /// <summary>The highest-value cards behind this label — where to look first when building the arm.</summary>
  [SerializedLabel("exampleCards")]
  public IReadOnlyList<string> ExampleCards { get; init; } = [];
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
