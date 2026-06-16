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

  /// <summary>The interaction-graph Plotly viz (label grammar | card-card expansion) — interactive HTML.</summary>
  public IItem<string> InteractionGraphHtml =>
    CreateItem(() => Item.Of<string>("InteractionGraphHtml")
      .Text()
      .AtPath($"{_basePath}/_08_Reporting/interaction-graph.html")
      .Build());
}
