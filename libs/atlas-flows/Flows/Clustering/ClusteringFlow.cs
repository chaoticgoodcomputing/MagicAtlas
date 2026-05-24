using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.Clustering;

/// <summary>
/// Discovers and labels semantic clusters of MTG oracle-text lines. Consumes the shared
/// <see cref="Catalog.EncodedTexts"/> encoder cache + <see cref="Catalog.LinePrimaryCanonicals"/>
/// supervision target. Produces:
/// </summary>
/// <list type="number">
/// <item><see cref="Catalog.ClusteringEmbeddings"/> — supervised HD→5D UMAP intermediate.
///   Downstream OracleEmbedding.ReduceToTwoD consumes this for the 2D atlas.</item>
/// <item><see cref="Catalog.ClusterAssignments"/> — per-line cluster IDs (HDBSCAN over 5D).</item>
/// <item><see cref="Catalog.ClusterLabels"/> — per-cluster c-TF-IDF labels.</item>
/// </list>
/// <remarks>
/// Cross-flow dependency: TagLabeling.DeriveLinePrimaryCanonical must run before ReduceToFiveD
/// so the supervision target exists.
/// </remarks>
public static class ClusteringFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Clustering", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "ReduceToFiveD",
        module: "Flows.Clustering.reduce_to_five_d",
        function: "reduce_to_five_d",
        input: (
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.LinePrimaryCanonicals,
          catalog.ClusteringConfig
        ),
        output: catalog.ClusteringEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ClusterEmbeddings",
        module: "Flows.Clustering.cluster_embeddings",
        function: "cluster_embeddings",
        input: (catalog.ClusteringEmbeddings, catalog.ClusteringConfig),
        output: catalog.ClusterAssignments,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "GenerateCTfIdfLabels",
        module: "Flows.Clustering.generate_ctfidf_labels",
        function: "generate_ctfidf_labels",
        input: (catalog.ClusterAssignments, catalog.OracleLines, catalog.ClusteringConfig),
        output: catalog.ClusterLabels,
        executor: executor
      );
    });
  }
}
