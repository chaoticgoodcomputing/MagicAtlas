using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;

namespace MagicAtlas.Flows.Discovery;

/// <summary>
/// Discovery flow — runs HDBSCAN on the UNSUPERVISED 5D embedding to surface candidate
/// archetypes that aren't yet captured by <see cref="Catalog.CanonicalArchetypes"/>.
/// </summary>
/// <remarks>
/// <para>
/// Sits outside the main pipeline (not registered as a dependency of TagLabeling or Reporting)
/// because it's curation-time tooling, not production attribution. Run it explicitly via
/// <c>--flow Discovery</c> when you want a fresh round of recommendations.
/// </para>
/// <para>
/// Requires <c>Clustering.ReduceToFiveD</c> to have run with <c>Umap5DSupervised=false</c> —
/// otherwise clusters would just rediscover the existing archetypes. The discover→curate→
/// validate loop:
/// <list type="number">
///   <item>Run this flow → <c>ArchetypeRecommendations</c> JSON written.</item>
///   <item>Run <c>scripts/review_archetype_recommendations.py</c> → markdown report.</item>
///   <item>User edits <c>canonical-archetypes.json</c> based on NEW/REFINE/MERGE entries.</item>
///   <item>Re-run the main pipeline + this flow for the next iteration.</item>
/// </list>
/// </para>
/// </remarks>
public static class DiscoveryFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("Discovery", pipeline =>
    {
      pipeline.AddPythonStep(
        label: "RecommendArchetypes",
        module: "Flows.Discovery.recommend_archetypes",
        function: "recommend_archetypes",
        input: (
          catalog.ClusteringEmbeddings,
          catalog.OracleLines,
          catalog.EncodedTexts,
          catalog.EncodedPrototypes,
          catalog.CanonicalArchetypes,
          catalog.ClusteringConfig
        ),
        output: catalog.ArchetypeRecommendations,
        executor: executor
      );
    });
  }
}
