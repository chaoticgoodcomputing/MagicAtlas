using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._00_Config.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.TagLabeling.Nodes;

namespace MagicAtlas.Flows.TagLabeling;

/// <summary>
/// Tag-anchored cluster labeling — the deterministic half. Two sources produce candidate tags
/// per cluster:
/// </summary>
/// <list type="number">
/// <item>Hand-curated exemplar archetypes embedded into centroids (the "config" track).</item>
/// <item>Scryfall otag-tagged card cohorts embedded into centroids (the "data" track).</item>
/// </list>
/// <remarks>
/// The LLM arbitration step (Qwen) lives in <c>MagicAtlas.Flows.QwenLabeling</c> and is
/// unregistered by default — its candidate input <see cref="Catalog.ClusterTagAffinity"/> is
/// produced here. Run <c>--flow TagLabeling</c> to materialize the candidates; opt into the LLM
/// pass separately.
/// </remarks>
public static class TagLabelingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("TagLabeling", pipeline =>
    {
      // ── Exemplar centroids (hand-curated archetypes from TagExemplars JSON) ──
      pipeline.AddPythonStep(
        label: "ComputeExemplarCentroids",
        module: "Flows.TagLabeling.compute_exemplar_centroids",
        function: "compute_exemplar_centroids",
        input: (catalog.TagExemplars, catalog.FineTunedEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.ExemplarTagCentroids,
        executor: executor
      );

      // ── Line-level canonical attributions (Pass 0 pattern + Pass 1 anchor + Pass 2a/2b inference) ──
      pipeline.AddPythonStep(
        label: "BuildCanonicalLineAssignments",
        module: "Flows.TagLabeling.build_canonical_line_assignments",
        function: "build_canonical_line_assignments",
        input: (
          catalog.ScryfallTagAssignments,
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.ScryfallTagCuration,
          catalog.KeywordVocabulary,
          catalog.ExemplarTagCentroids,
          catalog.TagLabelingConfig
        ),
        output: catalog.OracleLineCanonicalAssignments,
        executor: executor
      );

      // ── Scryfall centroids — mean-pool of line-level attributions, no card-level pollution. ──
      pipeline.AddPythonStep(
        label: "ComputeScryfallCentroids",
        module: "Flows.TagLabeling.compute_scryfall_centroids",
        function: "compute_scryfall_centroids",
        input: (
          catalog.OracleLineCanonicalAssignments,
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.ScryfallTagCuration
        ),
        output: catalog.ScryfallTagCentroids,
        executor: executor
      );

      // ── Primary canonical per line — the ground-truth derivative for supervision + reporting. ──
      pipeline.AddPythonStep(
        label: "DeriveLinePrimaryCanonical",
        module: "Flows.TagLabeling.derive_line_primary_canonical",
        function: "derive_line_primary_canonical",
        input: catalog.OracleLineCanonicalAssignments,
        output: catalog.LinePrimaryCanonicals,
        executor: executor
      );

      // ── Cluster-vs-canonical benchmark — scorecard for HDBSCAN quality vs ground truth. ──
      pipeline.AddPythonStep(
        label: "BenchmarkClustersVsCanonicals",
        module: "Flows.TagLabeling.benchmark_clusters_vs_canonicals",
        function: "benchmark_clusters_vs_canonicals",
        input: (catalog.ClusterAssignments, catalog.LinePrimaryCanonicals),
        output: catalog.ClusterCanonicalBenchmark,
        executor: executor
      );

      // ── 2D-placement scorecard: how well do canonicals project onto the atlas? ──
      pipeline.AddPythonStep(
        label: "EvaluateCanonicalPlacement",
        module: "Flows.TagLabeling.evaluate_canonical_placement",
        function: "evaluate_canonical_placement",
        input: (catalog.AtlasReportingPoints, catalog.LinePrimaryCanonicals),
        output: catalog.CanonicalPlacementMetrics,
        executor: executor
      );

      // ── Cross-level projection-quality scorecard (HD/5D/2D × 8 metrics in tidy long form). ──
      pipeline.AddPythonStep(
        label: "EvaluateProjectionQuality",
        module: "Flows.TagLabeling.evaluate_projection_quality",
        function: "evaluate_projection_quality",
        input: (
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.ClusteringEmbeddings,
          catalog.AtlasPoints,
          catalog.LinePrimaryCanonicals
        ),
        output: catalog.ProjectionQualityMetrics,
        executor: executor
      );

      // ── Per-cluster tag affinity (top-K candidates + sample lines) ──
      pipeline.AddPythonStep(
        label: "ComputeClusterTagAffinity",
        module: "Flows.TagLabeling.compute_cluster_tag_affinity",
        function: "compute_cluster_tag_affinity",
        input: (
          catalog.ClusterAssignments,
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.ExemplarTagCentroids,
          catalog.ScryfallTagCentroids,
          catalog.TagLabelingConfig
        ),
        output: catalog.ClusterTagAffinity,
        executor: executor
      );

      // ── Tag hierarchy export (C# step) — nested JSON + Mermaid view of the curation ──
      pipeline.AddStep<
        IEnumerable<ScryfallTagCanonical>,
        IEnumerable<TagHierarchyNode>,
        string
      >(
        label: "BuildTagHierarchy",
        transform: BuildTagHierarchyNode.Create(),
        inputs: catalog.ScryfallTagCuration,
        outputs: (catalog.TagHierarchy, catalog.TagHierarchyMermaid)
      );
    });
  }
}
