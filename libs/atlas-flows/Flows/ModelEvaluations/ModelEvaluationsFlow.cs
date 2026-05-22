using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.ModelEvaluations;

/// <summary>
/// Scores embedding-model variants against declarative pairwise-distance assertions defined in
/// <c>ModelEvaluationAssertions</c>. The first assertion the project ships with is
/// <c>evasion_class_flying_menace</c>: "Flying" and "Menace" centroids should sit closer to
/// each other than either does to "Trample" — a model that fails this hasn't learned the
/// evasion class.
/// </summary>
/// <remarks>
/// Each variant gets its own step instance reading the variant-specific catalog items
/// (<c>ClusteringEmbeddings</c> / <c>FineTunedClusteringEmbeddings</c>). The Python module
/// hosts both entry points; the variant label is stamped into the result rows so reports can
/// compare variants side-by-side after both flows have run.
/// </remarks>
public static class ModelEvaluationsFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("ModelEvaluations", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "EvaluateDefaultModel",
        module: "Flows.ModelEvaluations.evaluate_model",
        function: "evaluate_default",
        input: (
          catalog.ClusteringEmbeddings,
          catalog.OracleLines,
          catalog.ModelEvaluationAssertions,
          catalog.ModelEvaluationsConfig
        ),
        output: catalog.ModelEvaluation,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "EvaluateFineTunedModel",
        module: "Flows.ModelEvaluations.evaluate_model_finetuned",
        function: "evaluate_finetuned",
        input: (
          catalog.FineTunedClusteringEmbeddings,
          catalog.OracleLines,
          catalog.ModelEvaluationAssertions,
          catalog.ModelEvaluationsConfig
        ),
        output: catalog.FineTunedModelEvaluation,
        executor: executor
      );

      // ── Per-keyword diagnostic reports — one per model variant. ──
      pipeline.AddPythonStep(
        label: "KeywordClusterReport",
        module: "Flows.ModelEvaluations.keyword_cluster_report",
        function: "keyword_cluster_report",
        input: (
          catalog.KeywordVocabulary,
          catalog.OracleLines,
          catalog.AtlasPoints,
          catalog.ClusterAssignments,
          catalog.ClusterLabels,
          catalog.AtlasCardHoverInfo,
          catalog.ModelEvaluationsConfig
        ),
        output: catalog.KeywordClusterReport,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "FineTunedKeywordClusterReport",
        module: "Flows.ModelEvaluations.keyword_cluster_report_finetuned",
        function: "keyword_cluster_report_finetuned",
        input: (
          catalog.KeywordVocabulary,
          catalog.OracleLines,
          catalog.FineTunedAtlasPoints,
          catalog.FineTunedClusterAssignments,
          catalog.FineTunedClusterLabels,
          catalog.AtlasCardHoverInfo,
          catalog.ModelEvaluationsConfig
        ),
        output: catalog.FineTunedKeywordClusterReport,
        executor: executor
      );
    });
  }
}
