using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.Clustering;

/// <summary>
/// Discovers and labels semantic clusters of MTG oracle-text lines. Reads the shared
/// <see cref="Catalog.EncodedTexts"/> encoder cache (so the BERT encode never re-runs) plus
/// <see cref="Catalog.OracleLines"/> for the line-level identity, and produces:
/// </summary>
/// <list type="number">
/// <item><see cref="Catalog.ClusterAssignments"/> — per-line cluster IDs (HDBSCAN over a 5D
/// UMAP reduction of the encoded vectors; <c>-1</c> = noise).</item>
/// <item><see cref="Catalog.ClusterLabels"/> — generic per-cluster label records from c-TF-IDF.
/// Backend-agnostic schema; a future LLM labeler can write the same shape unchanged.</item>
/// </list>
/// <remarks>
/// Lives as its own flow rather than as tail steps of OracleEmbedding because (a) clustering
/// strategy is independent of the embedding strategy and we want to be able to swap either in
/// isolation, and (b) future model-comparison work runs multiple clustering configurations
/// against the same encoded texts.
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
        input: (catalog.OracleLines, catalog.EncodedTexts, catalog.ClusteringConfig),
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

      // ─── Fine-tuned variant ───
      pipeline.AddPythonStep(
        label: "ReduceToFiveDFineTuned",
        module: "Flows.Clustering.reduce_to_five_d_finetuned",
        function: "reduce_to_five_d_finetuned",
        input: (catalog.OracleLines, catalog.FineTunedEncodedTexts, catalog.ClusteringConfig),
        output: catalog.FineTunedClusteringEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ClusterEmbeddingsFineTuned",
        module: "Flows.Clustering.cluster_embeddings_finetuned",
        function: "cluster_embeddings_finetuned",
        input: (catalog.FineTunedClusteringEmbeddings, catalog.ClusteringConfig),
        output: catalog.FineTunedClusterAssignments,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "GenerateCTfIdfLabelsFineTuned",
        module: "Flows.Clustering.generate_ctfidf_labels_finetuned",
        function: "generate_ctfidf_labels_finetuned",
        input: (catalog.FineTunedClusterAssignments, catalog.OracleLines, catalog.ClusteringConfig),
        output: catalog.FineTunedClusterLabels,
        executor: executor
      );
    });
  }
}
