using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Reporting.Nodes;

namespace MagicAtlas.Flows.Reporting;

/// <summary>
/// Renders the explorer-mode atlas (index.html) — a standalone Plotly HTML scatter colored by
/// MTG color identity (WUBRG + colorless + multicolor-hollow). Joins AtlasPoints with OracleLines
/// (per-line text) and CardCoreData-derived hover metadata. No canonical/cluster overlay; that
/// would be exploiter-mode and belongs in MagicAST tooling, not on this map.
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
          catalog.ReportingConfig
        ),
        output: catalog.AtlasPlotHtml,
        executor: executor
      );
    });
  }
}
