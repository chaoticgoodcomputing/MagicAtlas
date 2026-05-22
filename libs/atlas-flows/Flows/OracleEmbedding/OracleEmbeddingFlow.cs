using Flowthru.Flow;
using Flowthru.Step.Python;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;

namespace MagicAtlas.Flows.OracleEmbedding;

/// <summary>
/// Produces 2D UMAP coordinates for every filtered card, using a BERT sentence-transformer over
/// oracle-text lines. Pipeline:
/// </summary>
/// <list type="number">
/// <item><b>ProjectOracleLines</b> (C#) — <see cref="CardCoreData"/> → <see cref="OracleLine"/>
/// (Arrow-safe per-line rows, stable hash-derived <c>LineId</c>).</item>
/// <item><b>EmbedOracleText</b> (Python) — deduplicates <c>OracleLines.Text</c>, encodes each
/// unique string once, writes <see cref="Catalog.EncodedTexts"/> as a persisted encoder cache.</item>
/// <item><b>ReduceToTwoD</b> (Python) — joins lines × encoded texts, applies pre-UMAP jitter,
/// runs UMAP → <see cref="AtlasPoint"/>.</item>
/// </list>
/// <remarks>
/// Fine-tuned variant mirrors the structure with its own <c>FineTunedEncodedTexts</c> cache and
/// <c>FineTunedAtlasPoints</c> output. Both variants share the same <c>OracleLines</c> input.
/// </remarks>
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
        input: (catalog.OracleLines, catalog.DefaultEmbeddingModel, catalog.OracleEmbeddingConfig),
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

      // ─── Fine-tuned variant ───
      pipeline.AddPythonStep(
        label: "EmbedOracleTextFineTuned",
        module: "Flows.OracleEmbedding.embed_oracle_text_finetuned",
        function: "embed_oracle_text_finetuned",
        input: (catalog.OracleLines, catalog.FineTunedEmbeddingModel, catalog.OracleEmbeddingConfig),
        output: catalog.FineTunedEncodedTexts,
        executor: executor
      );

      pipeline.AddPythonStep(
        label: "ReduceToTwoDFineTuned",
        module: "Flows.OracleEmbedding.reduce_to_2d_finetuned",
        function: "reduce_to_2d_finetuned",
        input: (catalog.OracleLines, catalog.FineTunedEncodedTexts, catalog.OracleEmbeddingConfig),
        output: catalog.FineTunedAtlasPoints,
        executor: executor
      );
    });
  }
}
