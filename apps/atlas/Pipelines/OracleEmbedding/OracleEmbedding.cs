using Flowthru.Core.Flows;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Steps;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Pipelines.OracleEmbedding;

/// <summary>
/// Produces 2D UMAP coordinates for every filtered card, using a BERT sentence-transformer
/// for oracle-text embeddings and UMAP for dimensionality reduction. Runs entirely in Python
/// (see <c>embed_and_reduce.py</c>); the output JSON is consumed by the atlas-api.
/// </summary>
public static class OracleEmbedding
{
  public static Flow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // C# projection: CardCoreData → OracleInput (Arrow-safe shape for the Python step).
      pipeline.AddStep(
        label: "ProjectOracleInput",
        transform: ProjectOracleInputNode.Create(),
        input: catalog.FilteredCardCoreData,
        output: catalog.OracleInputs
      );

      pipeline.AddPythonStep<IEnumerable<OracleInput>, IEnumerable<AtlasPoint>>(
        label: "EmbedAndReduce",
        module: "Pipelines.OracleEmbedding.embed_and_reduce",
        function: "embed_and_reduce",
        input: catalog.OracleInputs,
        output: catalog.AtlasPoints,
        executor: executor
      );
    });
  }
}
