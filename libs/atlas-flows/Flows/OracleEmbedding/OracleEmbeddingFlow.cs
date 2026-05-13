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
        label: "EmbedOracleText",
        module: "Flows.OracleEmbedding.embed_oracle_text",
        function: "embed_oracle_text",
        input: (catalog.OracleInputs, catalog.DefaultEmbeddingModel),
        output: catalog.BertEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ReduceToTwoD",
        module: "Flows.OracleEmbedding.reduce_to_2d",
        function: "reduce_to_2d",
        input: catalog.BertEmbeddings,
        output: catalog.AtlasPoints,
        executor: executor
      );

      // ─── Fine-tuned variant ───
      pipeline.AddPythonStep(
        label: "EmbedOracleTextFineTuned",
        module: "Flows.OracleEmbedding.embed_oracle_text",
        function: "embed_oracle_text_finetuned",
        input: (catalog.OracleInputs, catalog.FineTunedEmbeddingModel),
        output: catalog.FineTunedBertEmbeddings,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ReduceToTwoDFineTuned",
        module: "Flows.OracleEmbedding.reduce_to_2d",
        function: "reduce_to_2d_finetuned",
        input: catalog.FineTunedBertEmbeddings,
        output: catalog.FineTunedAtlasPoints,
        executor: executor
      );
    });
  }
}
