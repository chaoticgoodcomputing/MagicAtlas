using Flowthru.Data.Catalog;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Reporting layer (Layer 8). Holds the model-agnostic projections of the embedding output plus
/// the per-card metadata used for hover-over, and the final Plotly HTML produced by the Reporting
/// flow. The intermediates are persisted so that <c>--from AtlasReportingPoints</c> can iterate
/// on the plot without re-running the projection nodes.
/// </summary>
public partial class Catalog
{
  /// <summary>Atlas points stripped of variant-specific bits — pre-joined shape for the Plotly
  /// renderer.</summary>
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
}
