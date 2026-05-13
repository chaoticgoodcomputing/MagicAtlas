using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;

namespace MagicAtlas.Flows.OracleEmbedding;

/// <summary>
/// Produces 2D UMAP coordinates for every filtered card, using a BERT sentence-transformer
/// for oracle-text embeddings and UMAP for dimensionality reduction. Runs the embedding +
/// reduction step in Python (see <c>embed_and_reduce.py</c>); the C# projection upstream of it
/// just reshapes <see cref="CardCoreData"/> rows into the Arrow-safe <see cref="OracleInput"/>
/// shape the Python subprocess expects. Output JSON is consumed by the atlas-api.
/// </summary>
public static class OracleEmbeddingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("OracleEmbedding", pipeline =>
    {
      // C# projection: CardCoreData → OracleInput (Arrow-safe shape for the Python step).
      pipeline.AddStep<IEnumerable<CardCoreData>, IEnumerable<OracleInput>>(
        label: "ProjectOracleInput",
        transform: ProjectOracleInputNode.Create(),
        inputs: catalog.FilteredCardCoreData,
        outputs: catalog.OracleInputs
      );

      pipeline.AddPythonStep(
        label: "EmbedAndReduce",
        module: "Flows.OracleEmbedding.embed_and_reduce",
        function: "embed_and_reduce",
        input: catalog.OracleInputs,
        output: catalog.AtlasPoints,
        executor: executor
      );
    });
  }
}
