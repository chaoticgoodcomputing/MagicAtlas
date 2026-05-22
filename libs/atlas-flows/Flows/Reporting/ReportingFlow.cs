using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;
using MagicAtlas.Flows.Reporting.Nodes;

namespace MagicAtlas.Flows.Reporting;

/// <summary>
/// Renders interactive Plotly HTML scatters of the atlas embedding for both model variants.
/// Default-model output lands at <c>base.html</c>; fine-tuned-model output lands at
/// <c>index.html</c> (the primary atlas — fine-tuned mpnet outperforms default MiniLM on the
/// ModelEvaluations suite). Each variant joins four upstream inputs on <c>point_id</c> /
/// <c>card_id</c> in Python: reporting points, hover metadata, cluster assignments, cluster
/// labels. The hover metadata is model-agnostic and shared across both variants.
/// </summary>
/// <remarks>
/// The reporting input shapes (<see cref="ReportingPoint"/>, <c>ClusterAssignment</c>,
/// <c>ClusterLabel</c>) are deliberately model- and backend-agnostic — both variants reuse the
/// same projection node and Python impl; the two <c>@step</c> entries differ only in catalog
/// bindings.
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

      pipeline.AddStep<IEnumerable<AtlasPoint>, IEnumerable<ReportingPoint>>(
        label: "ProjectReportingPointsFineTuned",
        transform: ProjectReportingPointsNode.Create(),
        inputs: catalog.FineTunedAtlasPoints,
        outputs: catalog.FineTunedAtlasReportingPoints
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
          catalog.ClusterAssignments,
          catalog.ClusterLabels,
          catalog.ReportingConfig
        ),
        output: catalog.AtlasPlotHtml,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "BuildAtlasPlotFineTuned",
        module: "Flows.Reporting.build_atlas_plot_finetuned",
        function: "build_atlas_plot_finetuned",
        input: (
          catalog.FineTunedAtlasReportingPoints,
          catalog.OracleLines,
          catalog.AtlasCardHoverInfo,
          catalog.FineTunedClusterAssignments,
          catalog.FineTunedClusterLabels,
          catalog.ReportingConfig
        ),
        output: catalog.FineTunedAtlasPlotHtml,
        executor: executor
      );
    });
  }
}
