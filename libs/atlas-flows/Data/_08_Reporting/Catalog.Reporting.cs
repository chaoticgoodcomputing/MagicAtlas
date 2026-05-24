using Flowthru.Data.Catalog;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Reporting layer (Layer 8). Holds the model-agnostic projections of the embedding output plus
/// the per-card metadata used for hover-over, and the final Plotly HTML produced by the Reporting
/// flow. The intermediates are persisted (not memory-only) so that <c>--from
/// AtlasReportingPoints</c> can iterate on the plot without re-running the projection nodes — and
/// so that side-by-side comparison of multiple model outputs can later be done by swapping the
/// inputs files without touching the flow code.
/// </summary>
public partial class Catalog
{
  /// <summary>Atlas points stripped of variant-specific bits — just <c>(line_id, x, y, card_id,
  /// canonical_slug, …)</c>. Pre-joined shape for the Plotly renderer.</summary>
  public IItem<IEnumerable<ReportingPoint>> AtlasReportingPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<ReportingPoint>>("AtlasReportingPoints")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/atlas-reporting-points.json")
        .Build()
    );

  /// <summary>Per-card metadata used by the Plotly hover-over. Flat scalar fields only
  /// (Arrow-friendly).</summary>
  public IItem<IEnumerable<CardHoverInfo>> AtlasCardHoverInfo =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardHoverInfo>>("AtlasCardHoverInfo")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/atlas-card-hover-info.json")
        .Build()
    );

  /// <summary>The atlas Plotly HTML — the production artifact at <c>index.html</c>.</summary>
  public IItem<string> AtlasPlotHtml =>
    CreateItem(() =>
      Item.Of<string>("AtlasPlotHtml")
        .Text()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/index.html")
        .Build()
    );

  /// <summary>
  /// Mermaid flowchart rendering of the Scryfall-tag canonical hierarchy, wrapped in a
  /// fenced <c>mermaid</c> code block so GitHub/VS Code/IDE Markdown previewers render the
  /// diagram inline. Sibling output to <c>Catalog.TagHierarchy</c> (the nested-JSON form) —
  /// same data, human-readable tree.
  /// </summary>
  public IItem<string> TagHierarchyMermaid =>
    CreateItem(() =>
      Item.Of<string>("TagHierarchyMermaid")
        .Text()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/tag-hierarchy.md")
        .Build()
    );
}
