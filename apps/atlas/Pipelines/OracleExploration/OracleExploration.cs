using Flowthru.Pipelines;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.OracleExploration.Nodes;

namespace MagicAtlas.Pipelines.OracleExploration;

public static class OracleExploration
{
  public static Pipeline Create(Catalog catalog)
  {
    return PipelineBuilder.CreatePipeline(pipeline =>
    {
      pipeline.AddNode(
        label: "GenerateDuplicateOracleCounts",
        description: """
          Generates a dictionary of the most frequent, exactly-similar oracle texts to catch
          decorative/repetitive oracle entries prior to embedding generation
        """,
        transform: OracleTextFrequencyAnalysisNode.Create(),
        input: catalog.EmbeddingModelOracleInput,
        output: catalog.OracleTextFrequencyAnalysis
      );
    });
  }
}
