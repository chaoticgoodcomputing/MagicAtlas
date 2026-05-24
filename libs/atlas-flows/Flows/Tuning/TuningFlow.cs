using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.Tuning;

/// <summary>
/// UMAP hyperparameter sweep flow. Two independent sweep steps:
/// </summary>
/// <list type="number">
/// <item><b>SweepUmap2D</b> — unsupervised 5D→2D sweep (n_neighbors × min_dist).
/// Uses the existing ClusteringEmbeddings as fixed input; cheap (cuML on 5D points).</item>
/// <item><b>SweepUmap5D</b> — supervised HD→5D sweep (n_neighbors × min_dist × supervision_weight).
/// Each combo also runs a default 5D→2D so the scorecard reflects end-to-end impact.</item>
/// </list>
/// <remarks>
/// Tuning-only flow: outputs are sweep scorecards, not production atlas artifacts.
/// </remarks>
public static class TuningFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Tuning", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "SweepUmap2D",
        module: "Flows.Tuning.sweep_umap_2d",
        function: "sweep_umap_2d",
        input: (
          catalog.ClusteringEmbeddings,
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.LinePrimaryCanonicals,
          catalog.UmapSweep2DConfig
        ),
        output: catalog.UmapSweep2DResults,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "SweepUmap5D",
        module: "Flows.Tuning.sweep_umap_5d",
        function: "sweep_umap_5d",
        input: (
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.LinePrimaryCanonicals,
          catalog.UmapSweep5DConfig
        ),
        output: catalog.UmapSweep5DResults,
        executor: executor
      );
    });
  }
}
