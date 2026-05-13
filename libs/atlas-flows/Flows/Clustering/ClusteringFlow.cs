using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.Clustering;

/// <summary>
/// Discovers and labels semantic clusters of MTG ability fragments. Reads the shared
/// <see cref="Catalog.BertEmbeddings"/> intermediate (so it never re-runs the expensive BERT
/// encode) and produces two complementary artifacts:
/// </summary>
/// <list type="number">
/// <item><see cref="Catalog.ClusterAssignments"/> — per-point cluster IDs (HDBSCAN over a 5D
/// UMAP reduction of the embeddings; <c>-1</c> = noise).</item>
/// <item><see cref="Catalog.ClusterLabels"/> — generic per-cluster label records produced by a
/// c-TF-IDF backend (the BERTopic-style approach). The schema is backend-agnostic — a future
/// LLM labeler step can write the same shape with a different <c>source</c>, and downstream
/// consumers (Reporting, future API) need no code changes.</item>
/// </list>
/// <remarks>
/// Lives as its own flow rather than as tail steps of OracleEmbedding because (a) clustering
/// strategy is independent of the embedding strategy and we want to be able to swap either in
/// isolation, and (b) future model-comparison work will run multiple clustering configurations
/// against the same embeddings.
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
        input: catalog.BertEmbeddings,
        output: catalog.ClusteringEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ClusterEmbeddings",
        module: "Flows.Clustering.cluster_embeddings",
        function: "cluster_embeddings",
        input: catalog.ClusteringEmbeddings,
        output: catalog.ClusterAssignments,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "GenerateCTfIdfLabels",
        module: "Flows.Clustering.generate_ctfidf_labels",
        function: "generate_ctfidf_labels",
        input: (catalog.ClusterAssignments, catalog.OracleInputs),
        output: catalog.ClusterLabels,
        executor: executor
      );

      // ─── Fine-tuned variant ───
      pipeline.AddPythonStep(
        label: "ReduceToFiveDFineTuned",
        module: "Flows.Clustering.reduce_to_five_d",
        function: "reduce_to_five_d_finetuned",
        input: catalog.FineTunedBertEmbeddings,
        output: catalog.FineTunedClusteringEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ClusterEmbeddingsFineTuned",
        module: "Flows.Clustering.cluster_embeddings",
        function: "cluster_embeddings_finetuned",
        input: catalog.FineTunedClusteringEmbeddings,
        output: catalog.FineTunedClusterAssignments,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "GenerateCTfIdfLabelsFineTuned",
        module: "Flows.Clustering.generate_ctfidf_labels",
        function: "generate_ctfidf_labels_finetuned",
        input: (catalog.FineTunedClusterAssignments, catalog.OracleInputs),
        output: catalog.FineTunedClusterLabels,
        executor: executor
      );
    });
  }
}
