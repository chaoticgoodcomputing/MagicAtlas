using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// Structural diagnostic of the EMERGENT port-label graph — the "atoms of gameplay" graph the
/// two-layer cycle engine runs over (<c>libs/mast-interaction/docs/two-layer-cycle-engine.md</c>).
/// Where <see cref="PortLabelCensus"/> counts the label VOCABULARY (nodes) and
/// <see cref="PortGraphMetrics"/> counts the materialized instance graph's size, THIS report analyzes
/// the label graph's EDGE STRUCTURE: is it one giant strongly-connected blob (like the card-projection
/// SCC), or does it fragment into recognizable combo families once the universal "economy" connectors
/// (mana / tap) are cut? And which short elementary cycles bridge ≥2 resource families — the candidate
/// NOVEL combo archetypes the anchored reconstruction (DiceComboReport) never enumerates globally.
/// <para>
/// Nodes = distinct port LABELS (grouped from the materialized <c>PortEdge</c>s, one node per label —
/// the atom, not the per-card instance). A directed edge <c>A → B</c> exists iff some materialized
/// instance edge runs from a label-A port to a label-B port (the card-defined <em>wiring</em> edges
/// consume→emit, plus the rules-defined <em>arm</em> edges emit→consume). Diagnostic only — never a gate.
/// </para>
/// </summary>
[FlowthruSchema]
public partial record PortGraphAtlas
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  /// <summary>The card scope the label graph was materialized over (e.g. the CSB combo-card union).</summary>
  [SerializedLabel("scope")]
  public required string Scope { get; init; }

  /// <summary>Distinct cards that contributed ≥1 port to the materialized graph.</summary>
  [SerializedLabel("cardsInScope")]
  public int CardsInScope { get; init; }

  /// <summary>Distinct port-label NODES in the label graph (the atoms actually wired in scope).</summary>
  [SerializedLabel("labelNodes")]
  public int LabelNodes { get; init; }

  /// <summary>Distinct directed label→label EDGES (deduped from the instance edges).</summary>
  [SerializedLabel("labelEdges")]
  public int LabelEdges { get; init; }

  /// <summary>Emit-side label nodes (<c>emit:</c>) — the producers.</summary>
  [SerializedLabel("emitNodes")]
  public int EmitNodes { get; init; }

  /// <summary>Consume-side label nodes (pay/sac/tap/etb/ltb/trigger/cast/…) — the consumers.</summary>
  [SerializedLabel("consumeNodes")]
  public int ConsumeNodes { get; init; }

  // ── SCC of the FULL label graph (the "is it one blob?" measurement) ─────────────────────────────

  /// <summary>Strongly-connected-component count of the full label graph (Tarjan). 1 giant + many
  /// singletons ⇒ the same one-blob pathology as the card projection, but at atom scale.</summary>
  [SerializedLabel("sccCount")]
  public int SccCount { get; init; }

  /// <summary>Size (in label nodes) of the largest SCC — the core interaction blob.</summary>
  [SerializedLabel("largestSccSize")]
  public int LargestSccSize { get; init; }

  /// <summary>The largest SCC's member labels (capped) — the atoms mutually reachable through the economy.</summary>
  [SerializedLabel("largestScc")]
  public required IReadOnlyList<string> LargestScc { get; init; }

  /// <summary>The highest-degree label nodes (in+out) — the universal connectors / hubs.</summary>
  [SerializedLabel("topHubs")]
  public required IReadOnlyList<LabelDegree> TopHubs { get; init; }

  // ── Hub-cut experiment: remove the economy connectors, re-decompose ─────────────────────────────

  /// <summary>The resource families of the top-degree HUBS removed for the fragmentation experiment — the
  /// universal connectors the graph actually has (data-driven: the top <c>topHubs</c>, not a guessed family).</summary>
  [SerializedLabel("cutFamilies")]
  public required string CutFamilies { get; init; }

  /// <summary>How many label nodes (the top-degree hubs) were removed by the cut.</summary>
  [SerializedLabel("cutLabelCount")]
  public int CutLabelCount { get; init; }

  /// <summary>SCC count AFTER cutting the economy connectors — if it jumps, the blob was mana-glued.</summary>
  [SerializedLabel("sccCountAfterCut")]
  public int SccCountAfterCut { get; init; }

  /// <summary>Largest SCC size after the cut — if it collapses, the giant blob was an economy artifact.</summary>
  [SerializedLabel("largestSccSizeAfterCut")]
  public int LargestSccSizeAfterCut { get; init; }

  /// <summary>The multi-node SCC "islands" that survive the cut — the recognizable combo families
  /// (dice / blink / aristocrat / …), each with its dominant resource family + members.</summary>
  [SerializedLabel("islandsAfterCut")]
  public required IReadOnlyList<LabelIsland> IslandsAfterCut { get; init; }

  // ── Cross-family cycles: candidate combo archetypes ─────────────────────────────────────────────

  /// <summary>The label-cycle length bound used for the (bounded, sampled) elementary-cycle enumeration.</summary>
  [SerializedLabel("cycleLenBound")]
  public int CycleLenBound { get; init; }

  /// <summary>Distinct elementary label-cycles found within the bound (a SAMPLE — enumeration is capped).</summary>
  [SerializedLabel("boundedCyclesFound")]
  public int BoundedCyclesFound { get; init; }

  /// <summary>Of those, how many touch ≥2 resource families — the cross-family archetypes (novel-combo shapes).</summary>
  [SerializedLabel("crossFamilyCycles")]
  public int CrossFamilyCycles { get; init; }

  /// <summary>Of those, how many stay within one resource family — self-feeding single-resource engines.</summary>
  [SerializedLabel("singleFamilyCycles")]
  public int SingleFamilyCycles { get; init; }

  /// <summary>Distinct FAMILY-SIGNATURES among the cross-family cycles — the TRUE archetype diversity, once
  /// the label-facet multiplicity (Saproling-vs-Pentavite token variants of one shape) is collapsed away.</summary>
  [SerializedLabel("distinctArchetypes")]
  public int DistinctArchetypes { get; init; }

  /// <summary>A budget-limited SAMPLE of the cross-family archetypes read off the per-LABEL graph (grouped
  /// by family-signature). Superseded by <see cref="FamilyArchetypeCatalog"/> — kept for the concrete
  /// subtype flavor (real Saproling/Treasure rings) the family collapse erases.</summary>
  [SerializedLabel("sampleArchetypes")]
  public required IReadOnlyList<ArchetypeCycle> SampleArchetypes { get; init; }

  // ── Family-collapsed graph: the COMPLETE archetype catalog (atoms, not molecules) ────────────────

  /// <summary>Nodes of the family-collapsed graph — every label mapped to its resource family (the ~15-atom
  /// "periodic table": mana/token/sacrifice/death/dice/damage/…). One <c>token</c> node, not 50 subtypes.</summary>
  [SerializedLabel("familyNodes")]
  public int FamilyNodes { get; init; }

  /// <summary>Directed family→family edges (the label graph projected onto families).</summary>
  [SerializedLabel("familyEdges")]
  public int FamilyEdges { get; init; }

  /// <summary>Elementary-cycle length cap on the family graph (in families) — generous; real archetypes are
  /// ≤5 families, so this is effectively unbounded and only backstops a pathologically dense family graph.</summary>
  [SerializedLabel("familyCycleLenBound")]
  public int FamilyCycleLenBound { get; init; }

  /// <summary>Elementary cycles enumerated over the family graph — with the facet multiplicity collapsed
  /// away, this is small and exhaustively enumerable (no display budget, unlike the per-label pass).</summary>
  [SerializedLabel("familyCyclesFound")]
  public int FamilyCyclesFound { get; init; }

  /// <summary>True iff the family-graph enumeration ran to completion (no expansion-budget truncation) — so
  /// <see cref="FamilyArchetypeCatalog"/> is the COMPLETE archetype catalog, not a sample.</summary>
  [SerializedLabel("familyEnumComplete")]
  public bool FamilyEnumComplete { get; init; }

  /// <summary>Distinct family-SIGNATURES among the family cycles — the size of the complete archetype catalog.</summary>
  [SerializedLabel("familyArchetypes")]
  public int FamilyArchetypes { get; init; }

  /// <summary>The archetype catalog binned by family-count — how many distinct 2-family, 3-family, … archetypes
  /// exist. The small bins are the FUNDAMENTAL engines; the large bins are combinatorial elaborations
  /// (a fundamental loop with a redundant resource threaded in). Shows the combo space's shape at a glance.</summary>
  [SerializedLabel("familyArchetypesBySize")]
  public required IReadOnlyList<ArchetypeSizeBand> FamilyArchetypesBySize { get; init; }

  /// <summary>The COMPLETE cross-family archetype catalog: every distinct family-signature ring (≥2 families;
  /// within-family self-loops are excluded, being cost-payment like emit:mana→pay:mana, not feedback) the
  /// graph admits — one example ring + how many family-rings collapse into it, ranked by families bridged.
  /// The candidate combo-shape catalog to anchor + verify at instance level — the graph's answer to "what
  /// cross-resource infinite loops are structurally possible from the coverage we have."</summary>
  [SerializedLabel("familyArchetypeCatalog")]
  public required IReadOnlyList<ArchetypeCycle> FamilyArchetypeCatalog { get; init; }
}

/// <summary>A label node's in/out degree in the label graph, plus its resource family and card mass.</summary>
[FlowthruSchema]
public partial record LabelDegree
{
  [SerializedLabel("label")]
  public required string Label { get; init; }

  [SerializedLabel("family")]
  public required string Family { get; init; }

  [SerializedLabel("inDegree")]
  public int InDegree { get; init; }

  [SerializedLabel("outDegree")]
  public int OutDegree { get; init; }

  /// <summary>Distinct in-scope cards projecting this label (the centroid "mass").</summary>
  [SerializedLabel("cardsInScope")]
  public int CardsInScope { get; init; }
}

/// <summary>A strongly-connected island of the label graph that survives the economy cut — a combo family.</summary>
[FlowthruSchema]
public partial record LabelIsland
{
  [SerializedLabel("size")]
  public int Size { get; init; }

  /// <summary>The most common resource family among the island's labels — the family's name.</summary>
  [SerializedLabel("dominantFamily")]
  public required string DominantFamily { get; init; }

  [SerializedLabel("labels")]
  public required IReadOnlyList<string> Labels { get; init; }
}

/// <summary>A node of the family-collapsed "subway map" — a resource family (station), with its mass.</summary>
[FlowthruSchema]
public partial record FamilyNodeRow
{
  [SerializedLabel("family")]
  public required string Family { get; init; }

  /// <summary>Distinct in-scope cards that project ≥1 label in this family — the station's "ridership" (node size).</summary>
  [SerializedLabel("cards")]
  public int Cards { get; init; }

  /// <summary>Distinct port labels that collapse into this family (the subtype fan behind the station).</summary>
  [SerializedLabel("labels")]
  public int Labels { get; init; }
}

/// <summary>A directed edge of the family "subway map" — a line from one resource station to another.</summary>
[FlowthruSchema]
public partial record FamilyEdgeRow
{
  [SerializedLabel("from")]
  public required string From { get; init; }

  [SerializedLabel("to")]
  public required string To { get; init; }

  /// <summary>Underlying label edges of the ARM kind (emit→consume — the rules/physics: mana feeds a cost).</summary>
  [SerializedLabel("armWeight")]
  public int ArmWeight { get; init; }

  /// <summary>Underlying label edges of the WIRING kind (consume→emit — a card's own text: pay, then do).</summary>
  [SerializedLabel("wiringWeight")]
  public int WiringWeight { get; init; }

  /// <summary>True iff the reverse edge (To→From) also exists — the two stations form a fundamental
  /// two-family ENGINE (an infinite loop, e.g. blink↔etb). Highlighted on the map.</summary>
  [SerializedLabel("engine")]
  public bool Engine { get; init; }
}

/// <summary>How many distinct archetypes bridge exactly this many resource families (the catalog's shape).</summary>
[FlowthruSchema]
public partial record ArchetypeSizeBand
{
  [SerializedLabel("familyCount")]
  public int FamilyCount { get; init; }

  [SerializedLabel("archetypes")]
  public int Archetypes { get; init; }
}

/// <summary>A bounded elementary label-cycle — a candidate combo archetype (its label ring + families bridged).</summary>
[FlowthruSchema]
public partial record ArchetypeCycle
{
  /// <summary>Label hops in the ring.</summary>
  [SerializedLabel("length")]
  public int Length { get; init; }

  /// <summary>Distinct resource families the ring touches (1 ⇒ single-resource engine, ≥2 ⇒ cross-family combo).</summary>
  [SerializedLabel("familyCount")]
  public int FamilyCount { get; init; }

  /// <summary>How many enumerated facet-variant cycles collapse into this family-signature archetype.</summary>
  [SerializedLabel("occurrences")]
  public int Occurrences { get; init; }

  /// <summary>The families bridged, comma-joined (e.g. <c>blink, etb, dice</c>).</summary>
  [SerializedLabel("families")]
  public required string Families { get; init; }

  /// <summary>The label ring, arrow-joined (e.g. <c>emit:blink → etb:creature → … → emit:blink</c>).</summary>
  [SerializedLabel("ring")]
  public required string Ring { get; init; }
}
