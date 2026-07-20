using System.Text.Json.Nodes;
using Flowthru.Data.Catalog;
using MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

namespace MagicAtlas.Ast.Tests.Data;

/// <summary>Reporting layer: human- and agent-facing summaries.</summary>
public partial class Catalog
{
  /// <summary>
  /// The single output of the <c>MagicAstTriage</c> flow. Consumed directly by
  /// the <c>mast-tdd-loop</c> skill — agents read this file to pick their
  /// assigned gap. Path is the contract; do not rename without updating the
  /// skill.
  /// </summary>
  public IItem<TriageReport> TriageReport =>
    CreateItem(() => Item.Of<TriageReport>("TriageReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/triage-report.json")
      .Build());

  /// <summary>
  /// The output of the <c>InteractionTriage</c> flow: Commander Spellbook combos ranked by
  /// popularity and classified by blocking layer (parse vs reconstruction). The work-list for the
  /// interaction loop + the combo-priority overlay for the mast-tdd-loop.
  /// </summary>
  public IItem<InteractionTriageReport> InteractionTriageReport =>
    CreateItem(() => Item.Of<InteractionTriageReport>("InteractionTriageReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/interaction-triage-report.json")
      .Build());

  /// <summary>
  /// The combo-anchored pick surface: unparsed hub cards ranked by the combo-popularity value each
  /// gates, with sole-blocker counts, co-star neighborhood, and a block-reason split
  /// (parser-family vs the empty-oracle-text DATA gap). The demand-side complement to
  /// <see cref="InteractionTriageReport"/>'s allComboBlockingCards; a pick surface for the
  /// mast-tdd-loop, never a gate.
  /// </summary>
  public IItem<ComboAnchorReport> ComboAnchorReport =>
    CreateItem(() => Item.Of<ComboAnchorReport>("ComboAnchorReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/combo-anchor-report.json")
      .Build());

  /// <summary>The abstract label-level interaction graph (the known-families grammar, flattened) — left viz subplot.</summary>
  public IItem<IEnumerable<LabelEdgeRow>> LabelEdges =>
    CreateItem(() => Item.Of<IEnumerable<LabelEdgeRow>>("LabelEdges")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/label-edges.json")
      .Build());

  /// <summary>The materialized card-level interaction graph (engine edges over parse-ready combos) — right viz subplot.</summary>
  public IItem<IEnumerable<CardEdgeRow>> CardEdges =>
    CreateItem(() => Item.Of<IEnumerable<CardEdgeRow>>("CardEdges")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/card-edges.json")
      .Build());

  /// <summary>The reconstructed cycles (computed in C# by the PortGraphEngine, with cycle-level verdict tiers) — the viz's cycle subplot.</summary>
  public IItem<IEnumerable<CycleEdgeRow>> CycleEdges =>
    CreateItem(() => Item.Of<IEnumerable<CycleEdgeRow>>("CycleEdges")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/cycle-edges.json")
      .Build());

  /// <summary>Per-card node metadata (oracle text) for the viz hover — keyed by card name.</summary>
  public IItem<IEnumerable<PortNodeRow>> PortNodes =>
    CreateItem(() => Item.Of<IEnumerable<PortNodeRow>>("PortNodes")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/port-nodes.json")
      .Build());

  /// <summary>The port-label census (diagnostic): distinct-label counts across the parsed corpus —
  /// total + cycle-relevant + per-role + most-reused. Output of the <c>PortLabelCensus</c> flow; the
  /// card:label ratio is the health metric for the two-layer cycle engine.</summary>
  public IItem<PortLabelCensus> PortLabelCensus =>
    CreateItem(() => Item.Of<PortLabelCensus>("PortLabelCensus")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/port-label-census.json")
      .Build());

  /// <summary>Port-graph size/shape overview: node + edge counts of the materialized union interaction
  /// graph (and their tier/family/resource split). A quick read of how big + dense the reconstruction
  /// graph is; complements <see cref="PortLabelCensus"/> (whole-corpus label vocabulary).</summary>
  public IItem<PortGraphMetrics> PortGraphMetrics =>
    CreateItem(() => Item.Of<PortGraphMetrics>("PortGraphMetrics")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/port-graph-metrics.json")
      .Build());

  /// <summary>The dice-combo reconstruction report (diagnostic): every CSB die-roll combo reconstructed
  /// "as if the support cards were parsed" — best dice-cycle tier + hop count vs. product reach +
  /// cards-in-cycle + AST provenance, plus the engine-derived (novel) dice loops. Output of the
  /// <c>DiceComboReport</c> flow.</summary>
  public IItem<DiceComboReport> DiceComboReport =>
    CreateItem(() => Item.Of<DiceComboReport>("DiceComboReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/dice-combo-report.json")
      .Build());

  // ── CardAtlas data layer (D1–D4): the "shape → buildable" datasets. ──

  /// <summary>D1 — per-card deckbuilding metadata (colour identity, mana value, type line, port count).</summary>
  public IItem<IEnumerable<CardMetaRow>> CardMeta =>
    CreateItem(() => Item.Of<IEnumerable<CardMetaRow>>("CardMeta")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/card-meta.json")
      .Build());

  /// <summary>D1 — the card↔port index (one row per card, distinct port label; family + emit/consume side).</summary>
  public IItem<IEnumerable<CardPortRow>> CardPorts =>
    CreateItem(() => Item.Of<IEnumerable<CardPortRow>>("CardPorts")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/card-ports.json")
      .Build());

  /// <summary>The span-witness error-check (mast-loop Error-check track entry): parsed ports whose span
  /// text lacks the anchor its label asserts, routed to the golds that witness their stem. Corpus report
  /// (gitignored, never committed).</summary>
  public IItem<SpanWitnessReport> SpanWitnessReport =>
    CreateItem(() => Item.Of<SpanWitnessReport>("SpanWitnessReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/span-witness-report.json")
      .Build());

  /// <summary>The over-approximation report (ADR-0004 §6, modeled-dependency completeness): AST condition
  /// nodes the projection dropped, joined to the ports — and the GREENs — that rest on them. Fully derived
  /// by ablation; no hand-maintained register. Corpus report (gitignored, never committed).</summary>
  public IItem<OverApproximationReport> OverApproximationReport =>
    CreateItem(() => Item.Of<OverApproximationReport>("OverApproximationReport")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/over-approximation-report.json")
      .Build());

  /// <summary>D4 — per-combo reconstructed loops (named cards, family-signature, tier, result).</summary>
  public IItem<IEnumerable<ComboInstanceRow>> ComboInstances =>
    CreateItem(() => Item.Of<IEnumerable<ComboInstanceRow>>("ComboInstances")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/combo-instances.json")
      .Build());

  /// <summary>The wide reconstruction-recall measurement (co-produced with D4; measurement, never a gate).</summary>
  public IItem<ExtendedRecallReport> ExtendedRecall =>
    CreateItem(() => Item.Of<ExtendedRecallReport>("ExtendedRecall")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/extended-recall-report.json")
      .Build());

  /// <summary>D2 — the family subway map (stations + realized-combo-annotated directed lines).</summary>
  public IItem<ResourceGraph> ResourceGraph =>
    CreateItem(() => Item.Of<ResourceGraph>("ResourceGraph")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/resource-graph.json")
      .Build());

  /// <summary>D3 — the realized combo-shape catalog (family-signatures with ≥1 reconstructed combo).</summary>
  public IItem<ArchetypeCatalog> ArchetypeCatalog =>
    CreateItem(() => Item.Of<ArchetypeCatalog>("ArchetypeCatalog")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/archetype-catalog.json")
      .Build());

  /// <summary>The port-graph structural atlas (diagnostic): SCC decomposition + hub census + economy-cut
  /// fragmentation + cross-family cycle sample of the emergent port-LABEL graph. The edge-structure
  /// complement to <see cref="PortLabelCensus"/>; output of the <c>PortGraphAtlas</c> flow.</summary>
  public IItem<PortGraphAtlas> PortGraphAtlas =>
    CreateItem(() => Item.Of<PortGraphAtlas>("PortGraphAtlas")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/port-graph-atlas.json")
      .Build());

  /// <summary>The family "subway map" nodes (resource-family stations + card mass) — input to the viz step.</summary>
  public IItem<IEnumerable<FamilyNodeRow>> FamilyGraphNodes =>
    CreateItem(() => Item.Of<IEnumerable<FamilyNodeRow>>("FamilyGraphNodes")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/family-graph-nodes.json")
      .Build());

  /// <summary>The family "subway map" edges (directed family→family lines, arm/wiring-weighted, engine-flagged).</summary>
  public IItem<IEnumerable<FamilyEdgeRow>> FamilyGraphEdges =>
    CreateItem(() => Item.Of<IEnumerable<FamilyEdgeRow>>("FamilyGraphEdges")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/family-graph-edges.json")
      .Build());

  /// <summary>The family-graph "subway map" Plotly viz — resource stations, arm/wiring lines, engine loops highlighted.</summary>
  public IItem<string> FamilyGraphHtml =>
    CreateItem(() => Item.Of<string>("FamilyGraphHtml")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/family-graph.html")
      .Build());

  // ── Interaction-rollup (ADR-0003 §8, Stage 0b): golds → four rollup artifacts. ──

  /// <summary>The hand-authored interaction golds, loaded semi-structured as <see cref="JsonNode"/>s
  /// (port attr values are scalar-OR-object; declared-rule shapes vary per section). In-memory hand-off
  /// from the source step to the two rollup steps.</summary>
  public IItem<IEnumerable<JsonNode>> InteractionGolds =>
    CreateItem(() => Item.Of<IEnumerable<JsonNode>>("InteractionGolds").Memory().Build());

  /// <summary>The negotiated topology SCAFFOLD (ADR-0003 Stage 0a) — the DECLARED half of
  /// <c>port-topology</c> (kinds, supergroups, event verbs, the is-a spine, the closed attribute-axis set,
  /// aliases, and the six witness:sought holes). Loaded as a single semi-structured <see cref="JsonNode"/>;
  /// merged with the gold-witnessed half by the topology step.</summary>
  public IItem<JsonNode> InteractionScaffold =>
    CreateItem(() => Item.Of<JsonNode>("InteractionScaffold").Memory().Build());

  /// <summary>The interaction golds loaded for the SEPARATE corpus-gated <c>TopologyDemand</c> flow — a
  /// distinct memory slot from <see cref="InteractionGolds"/> so the two flows' merged DAGs keep unique
  /// step labels + item producers (Flowthru validates both globally). Reuses the same golds loader.</summary>
  public IItem<IEnumerable<JsonNode>> InteractionGoldsForDemand =>
    CreateItem(() => Item.Of<IEnumerable<JsonNode>>("InteractionGoldsForDemand").Memory().Build());

  /// <summary>Artifact 1 (lean) — the port universe: kinds map, stems, attribute axes.</summary>
  public IItem<PortTopology> PortTopology =>
    CreateItem(() => Item.Of<PortTopology>("PortTopology")
      .Json()
      .AtPath($"{_basePath}/../Fixtures/Interactions/rollup/port-topology.json")
      .Build());

  /// <summary>Artifact 1 (cited) — the port universe with per-stem witnesses.</summary>
  public IItem<PortTopology> PortTopologyCited =>
    CreateItem(() => Item.Of<PortTopology>("PortTopologyCited")
      .Json()
      .AtPath($"{_basePath}/../Fixtures/Interactions/rollup/port-topology.cited.json")
      .Build());

  /// <summary>Artifact 2 (lean) — the residual connection rules (polarity / match_policy / guards / bridges) + statuses.</summary>
  public IItem<PortInteractions> PortInteractions =>
    CreateItem(() => Item.Of<PortInteractions>("PortInteractions")
      .Json()
      .AtPath($"{_basePath}/../Fixtures/Interactions/rollup/port-interactions.json")
      .Build());

  /// <summary>Artifact 2 (cited) — the residual rules with witnesses + descriptions + CR citations + corroborations.</summary>
  public IItem<PortInteractions> PortInteractionsCited =>
    CreateItem(() => Item.Of<PortInteractions>("PortInteractionsCited")
      .Json()
      .AtPath($"{_basePath}/../Fixtures/Interactions/rollup/port-interactions.cited.json")
      .Build());

  /// <summary>The value-ranked demand overlay for the ADR-3 topology (corpus-gated DIAGNOSTIC): witnessed
  /// stems / sought holes / supergroups ranked by combo demand (gold popularity + combo-anchor payoff mass).
  /// Output of the <c>TopologyDemand</c> flow; corpus-derived, gitignored, never committed. Lives WITH the
  /// other corpus reports (not in the hermetic <c>Fixtures/Interactions/rollup/</c> dir).</summary>
  public IItem<PortTopologyDemand> PortTopologyDemand =>
    CreateItem(() => Item.Of<PortTopologyDemand>("PortTopologyDemand")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/port-topology-demand.json")
      .Build());

  /// <summary>The ADR-0004 §1 artifact classification manifest: every artifact on the declared surface
  /// classified Evidence / Derived / architectural-decision, with the ambiguous residue flagged for human
  /// classification. Output of the <c>ArtifactCensus</c> flow. Derivation is Flowthru's job; the GATE over
  /// this classification is <c>Tests/ArtifactCensus/ArtifactClassificationGateTests.cs</c>, which re-runs
  /// the same pure classifier rather than reading this (gitignored) file.</summary>
  public IItem<ArtifactCensus> ArtifactCensus =>
    CreateItem(() => Item.Of<ArtifactCensus>("ArtifactCensus")
      .Json()
      .AtPath($"{_basePath}/_08_Reporting/artifact-census.json")
      .Build());

  /// <summary>The interaction-graph Plotly viz (label grammar | card-card expansion) — interactive HTML.</summary>
  public IItem<string> InteractionGraphHtml =>
    CreateItem(() => Item.Of<string>("InteractionGraphHtml")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/interaction-graph.html")
      .Build());
}
