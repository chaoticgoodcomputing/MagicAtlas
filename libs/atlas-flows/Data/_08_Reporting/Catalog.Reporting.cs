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
  /// <summary>
  /// Atlas points with <c>text_type</c> stripped — just <c>(card_id, x, y)</c>. This is the
  /// model-agnostic shape that future embedding-model variants must produce to be rendered by the
  /// Reporting flow.
  /// </summary>
  public IItem<IEnumerable<ReportingPoint>> AtlasReportingPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<ReportingPoint>>("AtlasReportingPoints")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/atlas-reporting-points.json")
        .Build()
    );

  /// <summary>
  /// Per-card metadata used by the Plotly hover-over. Flat scalar fields only (Arrow-friendly).
  /// </summary>
  public IItem<IEnumerable<CardHoverInfo>> AtlasCardHoverInfo =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardHoverInfo>>("AtlasCardHoverInfo")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/atlas-card-hover-info.json")
        .Build()
    );

  /// <summary>
  /// Final standalone Plotly HTML document for the atlas embedding scatter, opened directly in a
  /// browser. Written as a single text file containing inline Plotly.js for offline viewing.
  /// </summary>
  public IItem<string> AtlasPlotHtml =>
    CreateItem(() =>
      Item.Of<string>("AtlasPlotHtml")
        .Text()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/index.html")
        .Build()
    );
}
