using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._00_Config.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.TagLabeling.Nodes;
using MagicAtlas.Services;

namespace MagicAtlas.Flows.QwenLabeling;

/// <summary>
/// Wraps the LLM arbitration step (cluster candidates → final label via Qwen) in its own flow
/// so it can be toggled on/off at the registration level without surgery on
/// <see cref="TagLabeling.TagLabelingFlow"/>. The Qwen pass is slow (~10 minutes per variant
/// on qwen3:4b at one cluster per request) and only worth running when label refinement is
/// the explicit goal — the curated exemplar centroids alone already produce useful labels for
/// the majority of clusters.
/// </summary>
/// <remarks>
/// <para>
/// To re-enable: add the line
/// <c>flowthru.RegisterFlow&lt;Catalog, IOllamaService&gt;("QwenLabeling", QwenLabelingFlow.Create)</c>
/// in <c>Program.cs</c> alongside the other flow registrations. Inputs (cluster affinity +
/// assignments + config) are produced by <c>TagLabeling</c>; run <c>--flow TagLabeling</c>
/// before <c>--flow QwenLabeling</c>.
/// </para>
/// </remarks>
public static class QwenLabelingFlow
{
  public static BuiltFlow Create(Catalog catalog, IOllamaService ollama)
  {
    return FlowBuilder.CreateFlow("QwenLabeling", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<ClusterTagAffinity>,
        IEnumerable<ClusterAssignment>,
        TagLabelingConfig,
        IEnumerable<ClusterLabel>
      >(
        label: "LabelClustersWithQwen",
        transform: LabelClustersWithQwenNode.Create(ollama),
        inputs: (catalog.ClusterTagAffinity, catalog.ClusterAssignments, catalog.TagLabelingConfig),
        outputs: catalog.TagAnchoredClusterLabels
      );
    });
  }
}
