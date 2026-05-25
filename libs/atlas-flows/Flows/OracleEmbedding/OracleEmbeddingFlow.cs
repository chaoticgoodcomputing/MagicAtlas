using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Flows.OracleEmbedding;

/// <summary>
/// Produces 2D UMAP coordinates for every filtered card, using the fine-tuned sentence-transformer
/// model over oracle-text lines. Explorer-mode pipeline:
/// </summary>
/// <list type="number">
/// <item><b>ProjectOracleLines</b> (C#) — <see cref="CardCoreData"/> → <see cref="OracleLine"/>
/// (Arrow-safe per-line rows, stable hash-derived <c>LineId</c>).</item>
/// <item><b>EmbedOracleText</b> (Python) — deduplicates <c>OracleLines.Text</c>, encodes each
/// unique string once via FineTunedEmbeddingModel, writes <see cref="Catalog.EncodedTexts"/>.</item>
/// <item><b>ReduceToTwoD</b> (Python) — unsupervised UMAP HD → 2D over <c>EncodedTexts</c>
/// (broadcast back to per-line rows), producing <see cref="AtlasPoint"/>.</item>
/// </list>
public static class OracleEmbeddingFlow
{
  public static BuiltFlow Create(Catalog catalog, IPythonExecutor executor)
  {
    return FlowBuilder.CreateFlow("OracleEmbedding", pipeline =>
    {
      pipeline.AddStep<
        IEnumerable<CardCoreData>,
        KeywordVocabulary,
        IEnumerable<OracleLine>,
        BarrelDetectionReport
      >(
        label: "ProjectOracleLines",
        transform: ProjectOracleLinesNode.Create(),
        inputs: catalog.FilteredCardCoreData,
        outputs: (catalog.KeywordVocabulary, catalog.OracleLines, catalog.BarrelDetectionReport)
      );

      pipeline.AddPythonStep(
        label: "EmbedOracleText",
        module: "Flows.OracleEmbedding.embed_oracle_text",
        function: "embed_oracle_text",
        input: (catalog.OracleLines, catalog.FineTunedEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.EncodedTexts,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ReduceToTwoD",
        module: "Flows.OracleEmbedding.reduce_to_2d",
        function: "reduce_to_2d",
        input: (catalog.OracleLines, catalog.EncodedTexts, catalog.OracleEmbeddingConfig),
        output: catalog.AtlasPoints,
        executor: executor
      );

      // Label-free fidelity scorecard — regression detector for the HD→2D projection. Cheap
      // (~few seconds on ~30k lines, sub-samples for the O(N²) bits) so wire it inline rather
      // than gating behind a separate "eval" flow.
      pipeline.AddPythonStep(
        label: "EvaluateAtlasFidelity",
        module: "Flows.OracleEmbedding.evaluate_atlas_fidelity",
        function: "evaluate_atlas_fidelity",
        input: (catalog.OracleLines, catalog.EncodedTexts, catalog.AtlasPoints),
        output: catalog.AtlasFidelityMetrics,
        executor: executor
      );
    });
  }
}
