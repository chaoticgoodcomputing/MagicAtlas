using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Reporting.Nodes;

namespace MagicAtlas.Flows.Reporting;

/// <summary>
/// Renders an interactive Plotly HTML scatter of the atlas embedding with a dropdown selector
/// for color scheme (cluster_id vs. WUBRG color identity). Joins four upstream inputs on
/// <c>point_id</c> / <c>card_id</c> in Python:
/// </summary>
/// <list type="bullet">
/// <item><see cref="Catalog.AtlasPoints"/> (via <see cref="Catalog.AtlasReportingPoints"/>) —
/// the 2D coordinates, <c>text_type</c> stripped.</item>
/// <item><see cref="Catalog.FilteredCardCoreData"/> (via <see cref="Catalog.AtlasCardHoverInfo"/>) —
/// flat per-card metadata for hover-over.</item>
/// <item><see cref="Catalog.ClusterAssignments"/> — per-point cluster id.</item>
/// <item><see cref="Catalog.ClusterLabels"/> — backend-agnostic cluster labels; whatever
/// labeler (c-TF-IDF, LLM, future) wrote this file, reporting just reads <c>label</c>.</item>
/// </list>
/// <remarks>
/// The reporting input shapes (<see cref="ReportingPoint"/>, <c>ClusterAssignment</c>,
/// <c>ClusterLabel</c>) are deliberately model- and backend-agnostic. Swapping embedding models
/// or label backends doesn't require flow changes — write the same shapes to the same catalog
/// items and re-run <c>--flow Reporting</c>.
/// </remarks>
public static class ReportingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Reporting", pipeline =>
    {
      pipeline.AddStep<IEnumerable<AtlasPoint>, IEnumerable<ReportingPoint>>(
        label: "ProjectReportingPoints",
        transform: ProjectReportingPointsNode.Create(),
        inputs: catalog.AtlasPoints,
        outputs: catalog.AtlasReportingPoints
      );

      pipeline.AddStep<IEnumerable<CardCoreData>, IEnumerable<CardHoverInfo>>(
        label: "ProjectCardHoverInfo",
        transform: ProjectCardHoverInfoNode.Create(),
        inputs: catalog.FilteredCardCoreData,
        outputs: catalog.AtlasCardHoverInfo
      );

      pipeline.AddPythonStep(
        label: "BuildAtlasPlot",
        module: "Flows.Reporting.build_atlas_plot",
        function: "build_atlas_plot",
        input: (
          catalog.AtlasReportingPoints,
          catalog.AtlasCardHoverInfo,
          catalog.ClusterAssignments,
          catalog.ClusterLabels
        ),
        output: catalog.AtlasPlotHtml,
        executor: executor
      );
    });
  }
}
