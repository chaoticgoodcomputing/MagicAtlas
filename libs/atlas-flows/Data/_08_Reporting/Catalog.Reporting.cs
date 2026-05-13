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
  /// Fine-tuned-model counterpart to <see cref="AtlasReportingPoints"/>. Same schema; sourced
  /// from <c>FineTunedAtlasPoints</c> so the Reporting flow can render both variants without
  /// duplicating downstream join logic.
  /// </summary>
  public IItem<IEnumerable<ReportingPoint>> FineTunedAtlasReportingPoints =>
    CreateItem(() =>
      Item.Of<IEnumerable<ReportingPoint>>("FineTunedAtlasReportingPoints")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/finetuned-atlas-reporting-points.json")
        .Build()
    );

  /// <summary>
  /// Default-model Plotly HTML. Written to <c>base.html</c> as the comparison baseline; the
  /// fine-tuned variant lands at <c>index.html</c> (see <see cref="FineTunedAtlasPlotHtml"/>).
  /// </summary>
  public IItem<string> AtlasPlotHtml =>
    CreateItem(() =>
      Item.Of<string>("AtlasPlotHtml")
        .Text()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/base.html")
        .Build()
    );

  /// <summary>
  /// Fine-tuned-model Plotly HTML. Written to <c>index.html</c> as the primary atlas — the
  /// fine-tuned mpnet is the better model per the ModelEvaluations suite.
  /// </summary>
  public IItem<string> FineTunedAtlasPlotHtml =>
    CreateItem(() =>
      Item.Of<string>("FineTunedAtlasPlotHtml")
        .Text()
        .AtPath($"{_basePath}/_08_Reporting/Datasets/index.html")
        .Build()
    );
}
