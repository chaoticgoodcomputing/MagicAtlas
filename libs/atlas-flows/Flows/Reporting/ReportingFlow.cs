using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Reporting.Nodes;

namespace MagicAtlas.Flows.Reporting;

/// <summary>
/// Renders the interactive Plotly HTML atlas (index.html). Joins AtlasPoints, OracleLines,
/// hover metadata, and all canonical attributions on line_id/card_id; colors by canonical
/// family and places annotations at canonical medoids. See build_atlas_plot.py for the
/// rendering choices.
/// </summary>
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
          catalog.OracleLines,
          catalog.AtlasCardHoverInfo,
          catalog.LinePrimaryCanonicals,
          catalog.OracleLineCanonicalAssignments,
          catalog.ScryfallTagCuration,
          catalog.ReportingConfig
        ),
        output: catalog.AtlasPlotHtml,
        executor: executor
      );
    });
  }
}
